using System;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public class MathResult : NodeModel
    {
        public MathResult()
        {
            Title = "MathResult";
        }

        public float Evaluate()
        {
            var port = this.GetInputPorts().FirstOrDefault();

            return this.GetValue(port);
        }

        public IPortModel DataIn0 { get; private set; }

        protected override void OnDefineNode()
        {
            DataIn0 = this.AddDataInputPort<float>("in");
        }
    }
}
