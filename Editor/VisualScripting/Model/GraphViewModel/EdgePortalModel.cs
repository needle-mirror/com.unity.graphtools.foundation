using System;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;

namespace UnityEditor.VisualScripting.GraphViewModel
{
    [Serializable]
    public abstract class EdgePortalModel : NodeModel, IEdgePortalModel
    {
        [SerializeField]
        int m_PortalID;
        [SerializeField]
        int m_EvaluationOrder;

        public int PortalID
        {
            get => m_PortalID;
            protected set => m_PortalID = value;
        }

        public int EvaluationOrder
        {
            get => m_EvaluationOrder;
            protected set => m_EvaluationOrder = value;
        }
    }

    [Serializable]
    public class ExecutionEdgePortalEntryModel : EdgePortalModel, IEdgePortalEntryModel, IHasMainExecutionInputPort
    {
        public IPortModel InputPort => ExecutionInputPort;
        public IPortModel ExecutionInputPort { get; private set; }

        public override string Title => $"Portal ({PortalID}) <<";

        protected override void OnDefineNode()
        {
            base.OnDefineNode();
            ExecutionInputPort = AddExecutionInputPort("");
        }
    }

    [Serializable]
    public class ExecutionEdgePortalExitModel : EdgePortalModel, IEdgePortalExitModel, IHasMainExecutionOutputPort
    {
        public IPortModel OutputPort => ExecutionOutputPort;
        public IPortModel ExecutionOutputPort { get; private set; }

        public override string Title => $">> Portal ({PortalID})";

        protected override void OnDefineNode()
        {
            base.OnDefineNode();
            ExecutionOutputPort = AddExecutionOutputPort("");
        }
    }

    [Serializable]
    public class DataEdgePortalEntryModel : EdgePortalModel, IEdgePortalEntryModel, IHasMainInputPort
    {
        public IPortModel InputPort { get; private set; }

        public override string Title => $"Portal ({PortalID}) <<";

        protected override void OnDefineNode()
        {
            base.OnDefineNode();
            InputPort = AddDataInputPort("", TypeHandle.Unknown);
        }
    }

    [Serializable]
    public class DataEdgePortalExitModel : EdgePortalModel, IEdgePortalExitModel, IHasMainOutputPort
    {
        public IPortModel OutputPort { get; private set; }

        public override string Title => $">> Portal ({PortalID})";

        protected override void OnDefineNode()
        {
            base.OnDefineNode();
            OutputPort = AddDataOutputPort("", TypeHandle.Unknown);
        }
    }
}
