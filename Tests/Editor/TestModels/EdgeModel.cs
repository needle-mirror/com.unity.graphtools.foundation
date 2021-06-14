using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels
{
    class EdgeModel : BasicModel.EdgeModel
    {
        [SerializeField, HideInInspector]
        GraphModel m_GraphModel;

        public override IGraphModel GraphModel => m_GraphModel;

        IPortModel m_FromPort;
        IPortModel m_ToPort;

        public override string EdgeLabel => m_EdgeLabel;

        public override IPortModel FromPort
        {
            get => m_FromPort;
            set => m_FromPort = value;
        }

        public override IPortModel ToPort
        {
            get => m_ToPort;
            set => m_ToPort = value;
        }

        //
        public override void SetPorts(IPortModel toPortModel, IPortModel fromPortModel)
        {
            base.SetPorts(toPortModel, fromPortModel);

            m_FromPort = fromPortModel;
            m_ToPort = toPortModel;
        }

        public void SetGraphModel(GraphModel graphModel)
        {
            m_GraphModel = graphModel;
        }
    }
}
