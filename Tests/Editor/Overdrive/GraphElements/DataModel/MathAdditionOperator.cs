using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    public class MathAdditionOperator : MathOperator
    {
        public void OnEnable()
        {
            name = "Add";
        }

        public override float Evaluate()
        {
            return (left != null ? left.Evaluate() : 0) + (right != null ? right.Evaluate() : 0);
        }
    }
}
