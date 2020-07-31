using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel
{
    [Serializable]
    public abstract class EdgePortalModel : NodeModel, IGTFEdgePortalModel, UnityEditor.GraphToolsFoundation.Overdrive.Model.IRenamable, ICloneable
    {
        [SerializeField]
        int m_EvaluationOrder;

        [SerializeReference]
        IDeclarationModel m_DeclarationModel;

        public IDeclarationModel DeclarationModel
        {
            get => m_DeclarationModel;
            set => m_DeclarationModel = value;
        }

        public override string Title => m_DeclarationModel == null ? "" : m_DeclarationModel.Title;

        public int EvaluationOrder
        {
            get => m_EvaluationOrder;
            protected set => m_EvaluationOrder = value;
        }

        public bool IsRenamable => true;

        public void Rename(string newName)
        {
            (DeclarationModel as Overdrive.Model.IRenamable)?.Rename(newName);
        }

        public IGTFGraphElementModel Clone()
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
    public class ExecutionEdgePortalEntryModel : EdgePortalModel, IGTFEdgePortalEntryModel, IHasMainExecutionInputPort
    {
        public IGTFPortModel InputPort => ExecutionInputPort;
        public IGTFPortModel ExecutionInputPort { get; private set; }

        protected override void OnDefineNode()
        {
            base.OnDefineNode();
            ExecutionInputPort = AddExecutionInputPort("");
        }
    }

    [Serializable]
    [MovedFrom(false, "UnityEditor.VisualScripting.GraphViewModel", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public class ExecutionEdgePortalExitModel : EdgePortalModel, IGTFEdgePortalExitModel, IHasMainExecutionOutputPort
    {
        public IGTFPortModel OutputPort => ExecutionOutputPort;
        public IGTFPortModel ExecutionOutputPort { get; private set; }

        protected override void OnDefineNode()
        {
            base.OnDefineNode();
            ExecutionOutputPort = AddExecutionOutputPort("");
        }
    }

    [Serializable]
    [MovedFrom(false, "UnityEditor.VisualScripting.GraphViewModel", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public class DataEdgePortalEntryModel : EdgePortalModel, IGTFEdgePortalEntryModel, IHasMainInputPort
    {
        public IGTFPortModel MainInputPort { get; private set; }
        public IGTFPortModel InputPort => MainInputPort;

        // Can't copy Data Entry portals as it makes no sense.
        public override bool IsCopiable => false;

        protected override void OnDefineNode()
        {
            base.OnDefineNode();
            MainInputPort = AddDataInputPort("", TypeHandle.Unknown);
        }
    }

    [Serializable]
    [MovedFrom(false, "UnityEditor.VisualScripting.GraphViewModel", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public class DataEdgePortalExitModel : EdgePortalModel, IGTFEdgePortalExitModel, IHasMainOutputPort
    {
        public IGTFPortModel MainOutputPort { get; private set; }
        public IGTFPortModel OutputPort => MainOutputPort;

        protected override void OnDefineNode()
        {
            base.OnDefineNode();
            MainOutputPort = AddDataOutputPort("", TypeHandle.Unknown);
        }

        public override bool CanCreateOppositePortal()
        {
            return !GraphModel.FindReferencesInGraph<IGTFEdgePortalEntryModel>(DeclarationModel).Any();
        }
    }
}
