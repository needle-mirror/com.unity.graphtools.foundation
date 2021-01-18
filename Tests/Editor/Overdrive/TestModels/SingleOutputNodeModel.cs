using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels
{
    class SingleOutputNodeModel : NodeModel, ISingleOutputPortNode
    {
        protected override void OnDefineNode()
        {
            this.AddDataOutputPort("", TypeHandle.Unknown);
        }

        public IPortModel OutputPort => Ports.First();

        public SingleOutputNodeModel()
        {
            this.SetCapability(Overdrive.Capabilities.Renamable, false);
        }
    }
}
