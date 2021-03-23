using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public class PowFunction : MathFunction
    {
        public PowFunction()
        {
            Title = "Pow";

            if (m_ParameterNames.Length == 0)
            {
                m_ParameterNames = new string[] { "f", "p" };
            }
        }

        public override float Evaluate()
        {
            return Mathf.Pow(GetParameterValue(0), GetParameterValue(1));
        }
    }
}
