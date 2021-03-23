using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public class SqrtFunction : MathFunction
    {
        public SqrtFunction()
        {
            Title = "SquareRoot";

            if (m_ParameterNames.Length == 0)
            {
                m_ParameterNames = new string[] { "f" };
            }
        }

        public override float Evaluate()
        {
            return Mathf.Sqrt(GetParameterValue(0));
        }
    }
}
