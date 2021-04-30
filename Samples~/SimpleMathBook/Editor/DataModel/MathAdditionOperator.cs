using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public class MathAdditionOperator : MathOperator
    {
        public MathAdditionOperator()
        {
            Title = "Add";
        }

        public override float Evaluate()
        {
            return left + right;
        }
    }
}
