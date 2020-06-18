using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel
{
    [Serializable]
    [MovedFrom(false, "UnityEditor.VisualScripting.GraphViewModel", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public abstract class EdgePortalModel : NodeModel, IEdgePortalModel, UnityEditor.GraphToolsFoundation.Overdrive.Model.IRenamable, ICloneable, IExposeTitleProperty, IHasVariableDeclarationModel
    {
        [SerializeField]
        int m_EvaluationOrder;

        [SerializeReference]
        VariableDeclarationModel m_DeclarationModel;

        public IVariableDeclarationModel DeclarationModel
        {
            get => m_DeclarationModel;
            set => m_DeclarationModel = (VariableDeclarationModel)value;
        }

        public string TitlePropertyName => "m_Name";

        public override string Title => m_DeclarationModel == null ? "" : m_DeclarationModel.Title;

        public int EvaluationOrder
        {
            get => m_EvaluationOrder;
            protected set => m_EvaluationOrder = value;
        }

        public bool IsRenamable => true;

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

        public virtual bool CanCreateOppositePortal()
        {
            return true;
        }
    }

    [Serializable]
    [MovedFrom(false, "UnityEditor.VisualScripting.GraphViewModel", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public class ExecutionEdgePortalEntryModel : EdgePortalModel, IEdgePortalEntryModel, IExecutionEdgePortalModel, IHasMainExecutionInputPort
    {
        public IPortModel InputPort => ExecutionInputPort;
        public IPortModel ExecutionInputPort { get; private set; }

        protected override void OnDefineNode()
        {
            base.OnDefineNode();
            ExecutionInputPort = AddExecutionInputPort("");
        }

        public IGTFPortModel GTFInputPort => InputPort as IGTFPortModel;
    }

    [Serializable]
    [MovedFrom(false, "UnityEditor.VisualScripting.GraphViewModel", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public class ExecutionEdgePortalExitModel : EdgePortalModel, IEdgePortalExitModel, IExecutionEdgePortalModel, IHasMainExecutionOutputPort
    {
        public IPortModel OutputPort => ExecutionOutputPort;
        public IPortModel ExecutionOutputPort { get; private set; }

        protected override void OnDefineNode()
        {
            base.OnDefineNode();
            ExecutionOutputPort = AddExecutionOutputPort("");
        }

        public IGTFPortModel GTFOutputPort => OutputPort as IGTFPortModel;
    }

    [Serializable]
    [MovedFrom(false, "UnityEditor.VisualScripting.GraphViewModel", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public class DataEdgePortalEntryModel : EdgePortalModel, IEdgePortalEntryModel, IDataEdgePortalModel, IHasMainInputPort
    {
        public IPortModel InputPort { get; private set; }

        // Can't copy Data Entry portals as it makes no sense.
        public override bool IsCopiable => false;

        protected override void OnDefineNode()
        {
            base.OnDefineNode();
            InputPort = AddDataInputPort("", TypeHandle.Unknown);
        }

        public IGTFPortModel GTFInputPort => InputPort as IGTFPortModel;
    }

    [Serializable]
    [MovedFrom(false, "UnityEditor.VisualScripting.GraphViewModel", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public class DataEdgePortalExitModel : EdgePortalModel, IEdgePortalExitModel, IDataEdgePortalModel, IHasMainOutputPort
    {
        public IPortModel OutputPort { get; private set; }

        protected override void OnDefineNode()
        {
            base.OnDefineNode();
            OutputPort = AddDataOutputPort("", TypeHandle.Unknown);
        }

        public override bool CanCreateOppositePortal()
        {
            return !((VariableDeclarationModel)DeclarationModel).FindReferencesInGraph().OfType<IEdgePortalEntryModel>().Any();
        }

        public IGTFPortModel GTFOutputPort => OutputPort as IGTFPortModel;
    }
}
