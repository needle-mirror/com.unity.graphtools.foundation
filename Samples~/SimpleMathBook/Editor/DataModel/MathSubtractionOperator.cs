using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public class MathSubtractionOperator : MathOperator
    {
        public MathSubtractionOperator()
        {
            Title = "Subtract";
        }

        public override float Evaluate()
        {
            return left - right;
        }
    }
}
