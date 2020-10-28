namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels
{
    class EdgeModel : BasicModel.EdgeModel
    {
        IPortModel m_FromPort;
        IPortModel m_ToPort;
        readonly IGraphModel m_GraphModel;

        public override IGraphModel GraphModel => m_GraphModel;

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

        public EdgeModel(IGraphModel graphModel = null)
        {
            m_GraphModel = graphModel;
        }

        //
        public override void SetPorts(IPortModel toPortModel, IPortModel fromPortModel)
        {
            m_FromPort = fromPortModel;
            m_ToPort = toPortModel;
        }
    }
}
