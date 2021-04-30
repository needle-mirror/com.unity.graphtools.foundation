using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public class MathMultiplicationOperator : MathOperator
    {
        public MathMultiplicationOperator()
        {
            Title = "Multiply";
        }

        public override float Evaluate()
        {
            return left * right;
        }
    }
}
