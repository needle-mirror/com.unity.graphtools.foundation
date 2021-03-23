using System;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public class MathResult : MathNode
    {
        public MathResult()
        {
            Title = "MathResult";
        }

        public override void ResetConnections()
        {
        }

        public override float Evaluate()
        {
            var port = this.GetInputPorts().FirstOrDefault();

            return GetValue(port);
        }

        public IPortModel DataIn0 { get; private set; }

        protected override void OnDefineNode()
        {
            DataIn0 = this.AddDataInputPort<float>("in");
        }
    }
}
