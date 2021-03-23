using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels
{
    class SingleInputNodeModel : NodeModel, ISingleInputPortNodeModel
    {
        protected override void OnDefineNode()
        {
            this.AddDataInputPort("", TypeHandle.Unknown);
        }

        public IPortModel InputPort => Ports.First();

        public SingleInputNodeModel()
        {
            this.SetCapability(Overdrive.Capabilities.Renamable, false);
        }
    }
}
