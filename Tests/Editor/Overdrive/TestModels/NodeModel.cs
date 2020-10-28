using UnityEngine.Assertions;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels
{
    class NodeModel : BasicModel.NodeModel, IRenamable
    {
        IGraphModel m_GraphModel;

        public override IGraphAssetModel AssetModel
        {
            get => m_GraphAssetModel;
            set
            {
                // We only accept a null asset in tests
                Assert.IsNull(value);
                m_GraphAssetModel = null;
            }
        }

        public override IGraphModel GraphModel => m_GraphModel;

        public void Rename(string newName)
        {
            if (!this.IsRenamable())
                return;

            Title = newName;
        }

        public override bool AllowSelfConnect => true;

        public NodeModel()
        {
            this.SetCapability(Overdrive.Capabilities.Renamable, true);
        }

        // Can't be on the property as we inherit a getter only GraphModel property.
        public void SetGraphModel(IGraphModel graphModel)
        {
            m_GraphModel = graphModel;
        }

        protected override IPortModel CreatePort(Direction direction, string portName, PortType portType,
            TypeHandle dataType, string portId, PortModelOptions options)
        {
            return new PortModel(GraphModel)
            {
                Title = portName ?? "",
                Direction = direction,
                PortType = portType,
                DataTypeHandle = dataType,
                NodeModel = this
            };
        }
    }
}
