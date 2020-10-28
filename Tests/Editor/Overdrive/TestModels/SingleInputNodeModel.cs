using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels
{
    class SingleInputNodeModel : NodeModel,  ISingleInputPortNode
    {
        protected override void OnDefineNode()
        {
            AddDataInputPort<PortModel>("", TypeHandle.Unknown);
        }

        public IPortModel InputPort => Ports.First();

        public SingleInputNodeModel()
        {
            this.SetCapability(Overdrive.Capabilities.Renamable, false);
        }
    }
}
