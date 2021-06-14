using System;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public class MathMultiplicationOperator : MathOperator
    {
        public override string Title
        {
            get => "Multiply";
            set { }
        }

        public override float Evaluate()
        {
            return Values.Aggregate<float, float>(1, (current, value) => current * value);
        }

        protected override void AddInputPorts()
        {
            for (var i = 0; i < InputPortCount; ++i)
                this.AddDataInputPort<float>("Factor " + (i + 1));
        }
    }
}
