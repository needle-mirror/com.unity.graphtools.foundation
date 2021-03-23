using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEditor.EditorCommon.Extensions;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Mode;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;

namespace UnityEditor.VisualScripting.Model
{
    [PublicAPI]
    [Serializable]
    public class FunctionCallNodeModel : NodeModel, IFunctionCallModel, IHasInstancePort
    {
        MethodBase m_MethodInfo;

        [SerializeField]
        TypeHandle m_DeclaringType;
        [SerializeField]
        string m_MethodInfoString;
        [SerializeField]
        TypeHandle[] m_TypeArguments;
        [SerializeField]
        bool m_IsStatic;
        [SerializeField]
        int m_OverloadHashCode;

        List<string> m_LastParametersAdded;

        public IEnumerable<string> ParametersNames => m_LastParametersAdded;

        public IPortModel GetPortForParameter(string parameterName)
        {
            return InputsById.TryGetValue(parameterName, out var portModel) ? portModel : null;
        }

        public override string Title => $"{VseUtility.GetTitle(m_MethodInfo)}";

        public bool IsConstructor => MethodInfo.IsConstructor;
        public TypeHandle DeclaringType => m_DeclaringType;

        public TypeHandle[] TypeArguments
        {
            get => m_TypeArguments;
            set => m_TypeArguments = value;
        }

        public MethodBase MethodInfo
        {
            get => m_MethodInfo;
            set
            {
                m_MethodInfo = value;
                m_DeclaringType = value?.DeclaringType.GenerateTypeHandle(Stencil) ?? TypeHandle.Unknown;
                m_MethodInfoString = value?.Name;
                m_IsStatic = MethodInfo.IsStatic;
                m_OverloadHashCode = TypeSystem.HashMethodSignature(value);
                if (value == null)
                    return;
                if (value.IsGenericMethod)
                    m_TypeArguments = value.GetGenericArguments().Where(t => String.IsNullOrEmpty(t.AssemblyQualifiedName)).
                        Select(t => t.GenerateTypeHandle(Stencil)).ToArray();
            }
        }

        public IPortModel InstancePort { get; private set; }

        PortModel m_OutputPort;
        public IPortModel OutputPort => m_OutputPort;

        public Type ReturnType { get; private set; }

        protected override void OnDefineNode()
        {
            // make sure these are null if not re-defined
            InstancePort = null;
            m_OutputPort = null;
            ReturnType = typeof(void);

            if (m_DeclaringType.IsValid && m_MethodInfoString != null)
            {
                // deprecation of name base search
                if (m_OverloadHashCode == 0)
                {
                    m_MethodInfo = TypeSystem.GetMethod(m_DeclaringType.Resolve(Stencil), m_MethodInfoString, m_IsStatic);
                    if (m_MethodInfo != null)
                        m_OverloadHashCode = TypeSystem.HashMethodSignature(m_MethodInfo);
                }
                else
                {
                    //TODO: Investigate why we cannot use SingleOrDefault here.
                    m_MethodInfo = m_DeclaringType.Resolve(Stencil)
                        .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | (m_IsStatic ? BindingFlags.Static : BindingFlags.Instance))
                        .Cast<MethodBase>()
                        .Concat(m_DeclaringType.Resolve(Stencil).GetConstructors())
                        .FirstOrDefault(m => TypeSystem.HashMethodSignature(m) == m_OverloadHashCode);
                }

                if (m_MethodInfo == null) // only log error if the data is kind-of making sense, not if if it's just empty
                    Debug.LogError("Serialization Error: Could not find method from MethodInfo:" + m_MethodInfoString);
            }

            if (m_MethodInfo != null)
            {
                if (!m_MethodInfo.IsStatic && !m_MethodInfo.IsConstructor)
                {
                    InstancePort = AddInstanceInput(m_MethodInfo.DeclaringType.GenerateTypeHandle(Stencil));
                }

                var parameters = m_MethodInfo.GetParameters();
                m_LastParametersAdded = new List<string>(parameters.Length);
                foreach (ParameterInfo parameter in parameters)
                {
                    string parameterName = parameter.Name;
                    m_LastParametersAdded.Add(parameterName);
                    AddDataInputPort(parameterName.Nicify(), parameter.ParameterType.GenerateTypeHandle(Stencil), parameterName);
                }

                if (m_MethodInfo.ContainsGenericParameters)
                {
                    var methodGenericArguments = m_MethodInfo.GetGenericArguments().Concat(m_MethodInfo?.DeclaringType?.GetGenericArguments() ?? Enumerable.Empty<Type>());
                    if (m_TypeArguments == null)
                        m_TypeArguments = methodGenericArguments.Select(o => o.GenerateTypeHandle(Stencil)).ToArray();
                    else
                    {
                        Type[] methodGenericArgumentsArray = methodGenericArguments.ToArray();
                        if (m_TypeArguments.Length != methodGenericArgumentsArray.Length)
                        {
                            // manual copy because of the implicit cast operator of TypeReference
                            Array.Resize(ref m_TypeArguments, methodGenericArgumentsArray.Length);
                            for (int i = m_TypeArguments.Length; i < methodGenericArgumentsArray.Length; i++)
                                m_TypeArguments[i] = methodGenericArgumentsArray[i].GenerateTypeHandle(Stencil);
                        }
                    }
                    if (m_TypeArguments.Length > 1)
                        Debug.LogWarning("Multiple generic types are not implemented yet");
                }

                var returnType = m_MethodInfo.GetReturnType();
                if (returnType.IsGenericType)
                {
                    returnType = returnType.GetGenericTypeDefinition();
                }

                if (returnType != typeof(void))
                {
                    m_OutputPort = AddDataOutputPort("result", returnType.GenerateTypeHandle(Stencil));
                    ReturnType = m_OutputPort.DataType.Resolve(Stencil);
                }
            }
        }

        public override void OnConnection(IPortModel selfConnectedPortModel, IPortModel otherConnectedPortModel)
        {
            try
            {
                if (selfConnectedPortModel == null || selfConnectedPortModel.Direction != Direction.Input)
                    return;

                Type otherConnectedType = otherConnectedPortModel?.DataType.Resolve(Stencil);
                if (m_MethodInfo.ContainsGenericParameters && otherConnectedType != null)
                {
                    var parameterIndex = m_LastParametersAdded.IndexOf(selfConnectedPortModel.UniqueId);
                    Type[] genericTypes = null;
                    if (!GenericsInferenceSolver.SolveTypeArguments(Stencil, m_MethodInfo, ref genericTypes, ref m_TypeArguments, otherConnectedType, parameterIndex))
                        return;
                    ApplyTypeArgumentsToInputs(genericTypes.First(), m_TypeArguments.First());

                    if (m_OutputPort != null)
                    {
                        Type outputType = GenericsInferenceSolver.InferReturnType(Stencil, m_MethodInfo, genericTypes, m_TypeArguments, m_OutputPort.DataType.Resolve(Stencil));
                        if (outputType != null)
                        {
                            m_OutputPort.DataType = outputType.GenerateTypeHandle(Stencil);
                            m_OutputPort.Name = outputType.FriendlyName();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Exception during OnConnection of node: {this}\n{e}");
                throw;
            }
        }

        void ApplyTypeArgumentsToInputs(Type genericType, TypeHandle solvedType)
        {
            ParameterInfo[] parameterInfos = MethodInfo.GetParameters();

            foreach (ParameterInfo parameter in parameterInfos)
            {
                if (GetPortForParameter(parameter.Name) is PortModel portModel) // null check + Cast
                {
                    if (parameter.ParameterType == genericType) // F(T)
                    {
                        portModel.DataType = solvedType;
                    }
                    else if (parameter.ParameterType.IsGenericType &&
                             parameter.ParameterType.GenericTypeArguments[0] == genericType) // F(List<T>)
                    {
                        portModel.DataType = parameter.ParameterType.GetGenericTypeDefinition().MakeGenericType(solvedType.Resolve(Stencil)).GenerateTypeHandle(Stencil);
                    }
                }
            }
        }
    }
}
