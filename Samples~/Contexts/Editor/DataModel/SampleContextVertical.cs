using System;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Contexts
{
    [Serializable]
    public class SampleContextVertical : SampleContext
    {
        public SampleContextVertical()
        {
            Title = "Context Vertical";
        }

        protected override void OnDefineNode()
        {
            base.OnDefineNode();
            this.AddExecutionInputPort("start", null, PortOrientation.Vertical);
            this.AddExecutionOutputPort("end", null, PortOrientation.Vertical);
        }
    }
}
