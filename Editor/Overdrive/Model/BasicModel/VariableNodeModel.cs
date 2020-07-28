using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using ICloneable = UnityEditor.GraphToolsFoundation.Overdrive.Model.ICloneable;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    [Serializable]
    //[MovedFrom(false, "UnityEditor.VisualScripting.Model", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    [MovedFrom("UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting")]
    public class VariableNodeModel : NodeModel, IGTFVariableNodeModel, IRenamable, ICloneable, IHasDeclarationModel, IHasMainOutputPort
    {
        const string k_MainPortName = "MainPortName";

        [SerializeReference]
        VariableDeclarationModel m_DeclarationModel;

        protected PortModel m_MainPortModel;

        public VariableType VariableType => VariableDeclarationModel.VariableType;

        public TypeHandle DataType => VariableDeclarationModel?.DataType ?? TypeHandle.Unknown;

        public override string DataTypeString => VariableDeclarationModel?.DataType.GetMetadata(Stencil).FriendlyName ?? string.Empty;

        public override string VariableString => DeclarationModel == null ? string.Empty : VariableDeclarationModel.IsExposed ? "Exposed variable" : "Variable";

        public override string Title => m_DeclarationModel == null ? "" : m_DeclarationModel.Title;

        public IDeclarationModel DeclarationModel
        {
            get => m_DeclarationModel;
            set => m_DeclarationModel = (VariableDeclarationModel)value;
        }

        public IGTFVariableDeclarationModel VariableDeclarationModel => DeclarationModel as IGTFVariableDeclarationModel;

        public IGTFPortModel InputPort => m_MainPortModel?.Direction == Direction.Input ? m_MainPortModel : null;

        public IGTFPortModel OutputPort => m_MainPortModel?.Direction == Direction.Output ? m_MainPortModel : null;

        public IGTFPortModel MainOutputPort => m_MainPortModel;

        public virtual bool IsRenamable => true;

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
                if (DataType == TypeHandle.ExecutionFlow)
                    m_MainPortModel = AddExecutionInputPort(null);
                else
                    m_MainPortModel = AddDataInputPort(null, DataType, k_MainPortName);
            }
            else
            {
                if (DataType == TypeHandle.ExecutionFlow)
                    m_MainPortModel = AddExecutionOutputPort(null);
                else
                    m_MainPortModel = AddDataOutputPort(null, DataType, k_MainPortName);
            }
        }

        public void Rename(string newName)
        {
            (DeclarationModel as IRenamable)?.Rename(newName);
        }

        public IGTFGraphElementModel Clone()
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
    }
}
