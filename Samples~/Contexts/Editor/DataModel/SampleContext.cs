using System;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Contexts
{
    [Serializable]
    public class SampleContext : ContextNodeModel
    {
        public SampleContext()
        {
            Title = "Context Horizontal";
        }

        protected override void OnDefineNode()
        {
            base.OnDefineNode();
            this.AddDataInputPort("in", TypeHandle.Float);
        }
    }
}
