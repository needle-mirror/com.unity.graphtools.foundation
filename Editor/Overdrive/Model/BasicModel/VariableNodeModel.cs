using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    [Serializable]
    //[MovedFrom(false, "UnityEditor.VisualScripting.Model", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    [MovedFrom("UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting")]
    public class VariableNodeModel : NodeModel, IVariableNodeModel, IRenamable, ICloneable, IHasMainOutputPort
    {
        const string k_MainPortName = "MainPortName";

        [SerializeReference]
        VariableDeclarationModel m_DeclarationModel;

        protected PortModel m_MainPortModel;

        // PF: remove base implementation
        public override string DataTypeString => VariableDeclarationModel?.DataType.GetMetadata(Stencil).FriendlyName ?? string.Empty;

        // PF: remove base implementation
        public override string VariableString => DeclarationModel == null ? string.Empty : VariableDeclarationModel.IsExposed ? "Exposed variable" : "Variable";

        public override string Title => m_DeclarationModel == null ? "" : m_DeclarationModel.Title;

        public IDeclarationModel DeclarationModel
        {
            get => m_DeclarationModel;
            set => m_DeclarationModel = (VariableDeclarationModel)value;
        }

        public IVariableDeclarationModel VariableDeclarationModel => DeclarationModel as IVariableDeclarationModel;

        public IPortModel InputPort => m_MainPortModel?.Direction == Direction.Input ? m_MainPortModel : null;

        public IPortModel OutputPort => m_MainPortModel?.Direction == Direction.Output ? m_MainPortModel : null;

        public IPortModel MainOutputPort => m_MainPortModel;

        public VariableNodeModel()
        {
            InternalInitCapabilities();
        }

        public virtual void UpdateTypeFromDeclaration()
        {
            if (DeclarationModel != null && m_MainPortModel != null)
                m_MainPortModel.DataTypeHandle = VariableDeclarationModel.DataType;

            // update connected nodes' ports colors/types
            if (m_MainPortModel != null)
                foreach (var connectedPortModel in m_MainPortModel.GetConnectedPorts())
                    connectedPortModel.NodeModel.OnConnection(connectedPortModel, m_MainPortModel);
        }

        protected override void OnDefineNode()
        {
            // used by macro outputs
            if (m_DeclarationModel != null /* this node */ && m_DeclarationModel.Modifiers.HasFlag(ModifierFlags.WriteOnly))
            {
                if (this.GetDataType() == TypeHandle.ExecutionFlow)
                    m_MainPortModel = AddExecutionInputPort(null);
                else
                    m_MainPortModel = AddDataInputPort(null, this.GetDataType(), k_MainPortName);
            }
            else
            {
                if (this.GetDataType() == TypeHandle.ExecutionFlow)
                    m_MainPortModel = AddExecutionOutputPort(null);
                else
                    m_MainPortModel = AddDataOutputPort(null, this.GetDataType(), k_MainPortName);
            }
        }

        public virtual void Rename(string newName)
        {
            if (!this.IsRenamable())
                return;

            (DeclarationModel as IRenamable)?.Rename(newName);
        }

        public IGraphElementModel Clone()
        {
            var decl = m_DeclarationModel;
            try
            {
                m_DeclarationModel = null;
                var clone = ICloneableExtensions.CloneUsingScriptableObjectInstantiate(this);
                clone.m_DeclarationModel = decl;
                return clone;
            }
            finally
            {
                m_DeclarationModel = decl;
            }
        }

        public override string Tooltip
        {
            get
            {
                var tooltip = $"{VariableString}";
                if (!string.IsNullOrEmpty(DataTypeString))
                    tooltip += $" of type {DataTypeString}";
                if (!string.IsNullOrEmpty(VariableDeclarationModel?.Tooltip))
                    tooltip += "\n" + VariableDeclarationModel.Tooltip;

                if (string.IsNullOrEmpty(tooltip))
                    return base.Tooltip;

                return tooltip;
            }
            set => base.Tooltip = value;
        }

        protected override void InitCapabilities()
        {
            base.InitCapabilities();
            InternalInitCapabilities();
        }

        void InternalInitCapabilities()
        {
            this.SetCapability(Overdrive.Capabilities.Renamable, true);
        }
    }
}
