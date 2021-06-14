using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels
{
    internal interface ITestNodeModel
    {
        void SetGraphModel(IGraphModel graphModel);
    }

    class NodeModel : BasicModel.NodeModel, IRenamable, ITestNodeModel
    {
        IGraphModel m_GraphModel;

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
            m_Capabilities.Add(Overdrive.Capabilities.Renamable);
        }

        // Can't be on the property as we inherit a getter only GraphModel property.
        public void SetGraphModel(IGraphModel graphModel)
        {
            m_GraphModel = graphModel;
        }

        public override IPortModel CreatePort(PortDirection direction, PortOrientation orientation, string portName,
            PortType portType, TypeHandle dataType, string portId, PortModelOptions options)
        {
            var port = new PortModel
            {
                Direction = direction,
                Orientation = orientation,
                PortType = portType,
                DataTypeHandle = dataType,
                Title = portName,
                UniqueName = portId,
                Options = options,
                NodeModel = this
            };
            port.SetGraphModel(GraphModel);
            return port;
        }
    }
}
