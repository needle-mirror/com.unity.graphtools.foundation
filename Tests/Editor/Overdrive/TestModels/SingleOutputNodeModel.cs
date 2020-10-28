using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels
{
    class SingleOutputNodeModel : NodeModel, ISingleOutputPortNode
    {
        protected override void OnDefineNode()
        {
            AddDataOutputPort<PortModel>("", TypeHandle.Unknown);
        }

        public IPortModel OutputPort => Ports.First();

        public SingleOutputNodeModel()
        {
            this.SetCapability(Overdrive.Capabilities.Renamable, false);
        }
    }
}
