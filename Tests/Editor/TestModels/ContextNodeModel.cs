using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels
{
    class ContextNodeModel : BasicModel.ContextNodeModel, ITestNodeModel
    {
        IGraphModel m_GraphModel;

        public override IGraphModel GraphModel => m_GraphModel;

        public override bool AllowSelfConnect => true;

        // Can't be on the property as we inherit a getter only GraphModel property.
        void ITestNodeModel.SetGraphModel(IGraphModel graphModel)
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
        public int ExeInputCount { get; set; }
        public int ExeOuputCount { get; set; }

        public int InputCount { get; set; }
        public int OuputCount { get; set; }

        protected override void OnDefineNode()
        {
            for (var i = 0; i < ExeInputCount; i++)
                this.AddExecutionInputPort("Exe In " + i);

            for (var i = 0; i < ExeOuputCount; i++)
                this.AddExecutionOutputPort("Exe Out " + i);

            for (var i = 0; i < InputCount; i++)
                this.AddDataInputPort("In " + i, TypeHandle.Unknown);

            for (var i = 0; i < OuputCount; i++)
                this.AddDataOutputPort("Out " + i, TypeHandle.Unknown);
        }
    }
}
