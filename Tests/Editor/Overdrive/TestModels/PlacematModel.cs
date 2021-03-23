namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels
{
    public class PlacematModel : BasicModel.PlacematModel
    {
        IGraphModel m_GraphModel;
        public override IGraphModel GraphModel => m_GraphModel;

        public override IGraphAssetModel AssetModel
        {
            get => m_AssetModel;
            // override setter to not throw when null
            set => m_AssetModel = (GraphAssetModel)value;
        }

        public void SetGraphModel(IGraphModel graphModel)
        {
            m_GraphModel = graphModel;
        }
    }
}
