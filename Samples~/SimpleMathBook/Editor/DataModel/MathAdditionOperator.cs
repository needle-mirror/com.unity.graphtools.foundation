using System;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public class MathAdditionOperator : MathOperator
    {
        public override string Title
        {
            get => "Add";
            set { }
        }

        public override float Evaluate()
        {
            return Values.Sum();
        }

        protected override void AddInputPorts()
        {
            for (var i = 0; i < InputPortCount; ++i)
                this.AddDataInputPort<float>("Term " + (i + 1));
        }
    }
}
