using System;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using ICloneable = UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel.ICloneable;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    [Serializable]
    [MovedFrom(false, "UnityEditor.VisualScripting.Model", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public class VariableNodeModel : NodeModel, IVariableModel, Overdrive.Model.IRenamable, IExposeTitleProperty, ICloneable, IHasVariableDeclarationModel
    {
        [SerializeReference]
        VariableDeclarationModel m_DeclarationModel;

        public VariableType VariableType => DeclarationModel.VariableType;

        public TypeHandle DataType => DeclarationModel?.DataType ?? TypeHandle.Unknown;

        public override string Title => m_DeclarationModel == null ? "" : m_DeclarationModel.Title;

        public IVariableDeclarationModel DeclarationModel
        {
            get => m_DeclarationModel;
            set => m_DeclarationModel = (VariableDeclarationModel)value;
        }

        public string TitlePropertyName => "m_Name";

        const string k_MainPortName = "MainPortName";

        protected PortModel m_MainPortModel;
        public IPortModel OutputPort => m_MainPortModel;

        public virtual void UpdateTypeFromDeclaration()
        {
            if (DeclarationModel != null && m_MainPortModel != null)
                m_MainPortModel.DataTypeHandle = DeclarationModel.DataType;

            // update connected nodes' ports colors/types
            if (m_MainPortModel != null)
                foreach (IPortModel connectedPortModel in m_MainPortModel.ConnectionPortModels)
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

        public virtual bool IsRenamable => true;

        public void Rename(string newName)
        {
            ((VariableDeclarationModel)DeclarationModel)?.SetNameFromUserName(newName);
        }

        public IGraphElementModel Clone()
        {
            var decl = m_DeclarationModel;
            try
            {
                m_DeclarationModel = null;
                var clone = GraphElementModelExtensions.CloneUsingScriptableObjectInstantiate(this);
                clone.m_DeclarationModel = decl;
                return clone;
            }
            finally
            {
                m_DeclarationModel = decl;
            }
        }

        public IGTFPortModel GTFInputPort => m_MainPortModel?.Direction == Direction.Input ? m_MainPortModel as IGTFPortModel : null;
        public IGTFPortModel GTFOutputPort => m_MainPortModel?.Direction == Direction.Output ? m_MainPortModel as IGTFPortModel : null;
    }
}
