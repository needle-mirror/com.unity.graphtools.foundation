using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.VisualScripting.Model
{
    [Serializable]
    public class FunctionRefCallNodeModel : NodeModel, IObjectReference, IExposeTitleProperty, IHasInstancePort,
        IFunctionCallModel
    {
        [SerializeField]
        SerializableGUID m_FunctionModelGuid;

        List<string> m_LastParametersAdded;

        public override string Title
        {
            get
            {
                if (Function)
                {
                    return (Function.GraphModel != GraphModel ? Function.GraphModel.Name + "." : string.Empty) +
                        Function.Title;
                }
                return $"<{base.Title}>";
            }
        }

        public FunctionModel Function
        {
            get
            {
                if (GraphModel != null && GraphModel.NodesByGuid.TryGetValue(m_FunctionModelGuid, out var functionModel))
                    return functionModel as FunctionModel;
                return null;
            }
            set
            {
                AssetModel = (GraphAssetModel)value?.AssetModel;
                m_FunctionModelGuid = value?.Guid ?? default;
            }
        }

        public Object ReferencedObject => Function?.GraphModel.AssetModel as Object;
        public string TitlePropertyName => "m_Name";
        public IPortModel InstancePort { get; private set; }
        public IPortModel OutputPort { get; private set; }
        public IEnumerable<string> ParametersNames => m_LastParametersAdded;

        public override IReadOnlyList<IPortModel> InputsByDisplayOrder
        {
            get
            {
                DefineNode(); // the macro definition might have been modified
                return base.InputsByDisplayOrder;
            }
        }

        public override IReadOnlyList<IPortModel> OutputsByDisplayOrder
        {
            get
            {
                DefineNode(); // the macro definition might have been modified
                return base.OutputsByDisplayOrder;
            }
        }

        public IPortModel GetPortForParameter(string parameterName)
        {
            return InputsById.TryGetValue(parameterName, out var portModel) ? portModel : null;
        }

        protected override void OnDefineNode()
        {
            var functionModel = Function;
            if (!functionModel)
                return;

            InstancePort = null;
            if (functionModel.IsInstanceMethod)
                throw new InvalidOperationException("Function references cannot be instance methods");

            m_LastParametersAdded = new List<string>(functionModel.FunctionParameterModels.Count());
            foreach (var parameter in functionModel.FunctionParameterModels)
            {
                AddDataInputPort(parameter.Name, parameter.DataType);
                m_LastParametersAdded.Add(parameter.Name);
            }

            var voidType = typeof(void).GenerateTypeHandle(Stencil);
            OutputPort = functionModel.ReturnType != voidType
                ? AddDataOutputPort("result", functionModel.ReturnType)
                : null;
        }
    }
}
