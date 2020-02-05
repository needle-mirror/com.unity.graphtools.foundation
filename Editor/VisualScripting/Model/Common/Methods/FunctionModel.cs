using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEditor.VisualScripting.Model
{
    [Serializable]
    public class FunctionModel : StackBaseModel, IFunctionModel, IRenamableModel, IHasMainOutputPort
    {
        [SerializeReference]
        protected List<VariableDeclarationModel> m_FunctionVariableModels = new List<VariableDeclarationModel>();

        [SerializeReference]
        protected List<VariableDeclarationModel> m_FunctionParameterModels = new List<VariableDeclarationModel>();

        [SerializeField]
        TypeHandle m_ReturnType;

        [SerializeField]
        bool m_EnableProfiling;

        public IEnumerable<IVariableDeclarationModel> FunctionVariableModels => m_FunctionVariableModels;
        public IEnumerable<IVariableDeclarationModel> FunctionParameterModels => m_FunctionParameterModels;

        public IList<VariableDeclarationModel> VariableDeclarations => m_FunctionVariableModels;

        public IList<VariableDeclarationModel> FunctionParameters => m_FunctionParameterModels;

        public string CodeTitle => TypeSystem.CodifyString(Title);

        public override string IconTypeString => "typeFunction";

        public TypeHandle ReturnType
        {
            get => m_ReturnType;
            set => m_ReturnType = value;
        }

        public virtual bool IsEntryPoint => true;
        public virtual bool AllowChangesToModel => !(this is IEventFunctionModel);
        public virtual bool AllowMultipleInstances => true;

        public override IFunctionModel OwningFunctionModel => this;

        public bool EnableProfiling
        {
            get => m_EnableProfiling;
            set => m_EnableProfiling = value;
        }

        public virtual bool HasReturnType => true;

        // TODO : Refactor needed. Use AccessibilityFlags instead
        public virtual bool IsInstanceMethod => false;
        public IPortModel OutputPort { get; protected set; }


        public VariableDeclarationModel CreateFunctionVariableDeclaration(string variableName, TypeHandle variableType)
        {
            VariableDeclarationModel decl = VariableDeclarationModel.Create(variableName, variableType, false, (GraphModel)GraphModel, VariableType.FunctionVariable, ModifierFlags.None, this);
            m_FunctionVariableModels.Add(decl);
            return decl;
        }

        public VariableDeclarationModel CreateAndRegisterFunctionParameterDeclaration(string parameterName, TypeHandle parameterType)
        {
            VariableDeclarationModel decl = CreateParameterDeclaration(parameterName, parameterType);
            RegisterFunctionParameterDeclaration(decl);
            return decl;
        }

        public void RegisterFunctionParameterDeclaration(VariableDeclarationModel decl)
        {
            if (!m_FunctionParameterModels.Contains(decl))
                m_FunctionParameterModels.Add(decl);
        }

        public VariableDeclarationModel FindOrCreateParameterDeclaration(string parameterName, TypeHandle parameterType)
        {
            VariableDeclarationModel existing = FindFunctionParameterDeclaration(parameterName);
            if (existing == null)
                existing = CreateParameterDeclaration(parameterName, parameterType);
            return existing;
        }

        VariableDeclarationModel CreateParameterDeclaration(string parameterName, TypeHandle parameterType)
        {
            return VariableDeclarationModel.Create(parameterName, parameterType, false,
                (GraphModel)GraphModel, VariableType.FunctionParameter, ModifierFlags.None, this);
        }

        VariableDeclarationModel FindFunctionParameterDeclaration(string parameterName)
        {
            return m_FunctionParameterModels.FirstOrDefault(p => p.VariableName == parameterName);
        }

        public List<VariableDeclarationModel> DuplicateFunctionVariableDeclarations(List<IVariableDeclarationModel> variableDeclarationModels)
        {
            List<VariableDeclarationModel> duplicatedModels = new List<VariableDeclarationModel>();
            foreach (VariableDeclarationModel variableDeclarationModel in variableDeclarationModels.Cast<VariableDeclarationModel>())
            {
                if ((variableDeclarationModel.variableFlags & VariableFlags.Generated) != 0)
                    continue;

                string uniqueName = ((VSGraphModel)GraphModel).GetUniqueName(variableDeclarationModel.Name);

                if (variableDeclarationModel.VariableType == VariableType.FunctionParameter)
                {
                    VariableDeclarationModel decl = VariableDeclarationModel.CreateNoUndoRecord(
                        uniqueName,
                        variableDeclarationModel.DataType,
                        false,
                        (GraphModel)GraphModel,
                        VariableType.FunctionParameter,
                        ModifierFlags.None,
                        this,
                        variableDeclarationModel.variableFlags,
                        variableDeclarationModel.InitializationModel);
                    m_FunctionParameterModels.Add(decl);
                    duplicatedModels.Add(decl);
                }
                else
                {
                    VariableDeclarationModel decl = VariableDeclarationModel.CreateNoUndoRecord(
                        uniqueName,
                        variableDeclarationModel.DataType,
                        false,
                        (GraphModel)GraphModel,
                        VariableType.FunctionVariable,
                        ModifierFlags.None,
                        this,
                        VariableFlags.None,
                        variableDeclarationModel.InitializationModel);
                    m_FunctionVariableModels.Add(decl);
                    duplicatedModels.Add(decl);
                }
            }

            return duplicatedModels;
        }

        public void RemoveFunctionVariableDeclaration(VariableDeclarationModel decl)
        {
            Assert.AreEqual(decl.FunctionModel, this);
            m_FunctionVariableModels.Remove(decl);
        }

        public void RemoveFunctionParameterDeclaration(VariableDeclarationModel param)
        {
            Assert.AreEqual(param.FunctionModel, this);
            m_FunctionParameterModels.Remove(param);
        }

        protected override void OnPreDefineNode()
        {
            base.OnPreDefineNode();

            foreach (var functionVariableModel in m_FunctionVariableModels)
                functionVariableModel.Owner = this;

            foreach (var functionVariableModel in m_FunctionParameterModels)
                functionVariableModel.Owner = this;
        }

        protected override void OnDefineNode()
        {
            if (!m_ReturnType.IsValid)
                ReturnType = typeof(void).GenerateTypeHandle(Stencil);

            OutputPort = AddExecutionOutputPort(null);

            CreateLoopVariables(null);
        }

        public void CreateLoopVariables(IPortModel connectedPortModel)
        {
            VariableCreator c = new VariableCreator(this);
            OnCreateLoopVariables(c, connectedPortModel);
            c.Flush();
        }

        protected virtual void OnCreateLoopVariables(VariableCreator variableCreator, IPortModel connectedPortModel) {}

        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        [SuppressMessage("ReSharper", "BaseObjectGetHashCodeCallInGetHashCode")]
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                foreach (var parameterModel in m_FunctionParameterModels)
                {
                    if (parameterModel == null)
                        hashCode = (hashCode * 397) ^ (parameterModel.GetHashCode());
                }
                hashCode = (hashCode * 397) ^ m_ReturnType.GetHashCode();
                return hashCode;
            }
        }

        public void Rename(string newName)
        {
            Title = ((VSGraphModel)GraphModel).GetUniqueName(newName);
            ((VSGraphModel)GraphModel).LastChanges.RequiresRebuild = true;
        }

#if UNITY_2020_1_OR_NEWER
        public override CapabilityFlags Capabilities => CapabilityFlags.Selectable | CapabilityFlags.Deletable |
        CapabilityFlags.Movable | CapabilityFlags.Renamable | CapabilityFlags.Copiable;
#else
        public override CapabilityFlags Capabilities => CapabilityFlags.Selectable | CapabilityFlags.Deletable |
        CapabilityFlags.Movable | CapabilityFlags.Renamable;
#endif

        public void ClearVariableDeclarations()
        {
            m_FunctionVariableModels.Clear();
        }

        public void ClearParameterDeclarations()
        {
            m_FunctionParameterModels.Clear();
        }

        public IEnumerable<FunctionRefCallNodeModel> FindFunctionUsages(IGraphModel previousStateCurrentGraphModel)
        {
            return previousStateCurrentGraphModel.NodeModels
                .OfType<StackBaseModel>()
                .SelectMany(s => s.NodeModels)
                .OfType<FunctionRefCallNodeModel>()
                .Where(f => this == (IFunctionModel)f.Function);
        }
    }
}
