using System;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public abstract class MathOperator : MathNode
    {

        public float left
        {
            get
            {
                IPortModel leftPort = this.GetInputPorts().FirstOrDefault();
                if (leftPort == null) return 0;

                return GetValue(leftPort);
            }
        }

        public float right
        {
            get
            {
                IPortModel rightPort = this.GetInputPorts().Skip(1).FirstOrDefault();
                if (rightPort == null) return 0;

                return GetValue(rightPort);
            }
        }

        public override void ResetConnections()
        {
        }

        public IPortModel DataIn0 { get; private set; }
        public IPortModel DataIn1 { get; private set; }
        public IPortModel DataOut1 { get; private set; }

        protected override void OnDefineNode()
        {
            DataIn0 = this.AddDataInputPort<float>("left");
            DataIn1 = this.AddDataInputPort<float>("right");
            DataOut1 = this.AddDataOutputPort<float>("out");
        }
    }
}
