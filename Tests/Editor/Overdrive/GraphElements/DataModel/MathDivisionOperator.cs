using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    public class MathDivisionOperator : MathOperator
    {
        public void OnEnable()
        {
            name = "Divide";
        }

        public override float Evaluate()
        {
            return (left != null ? left.Evaluate() : 0) / (right != null ? right.Evaluate() : Single.NaN);
        }
    }
}
