using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public class MathDivisionOperator : MathOperator
    {
        public MathDivisionOperator()
        {
            Title = "Divide";
        }

        public override float Evaluate()
        {
            return left / right;
        }
    }
}
