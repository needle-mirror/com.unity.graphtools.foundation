using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public class ExpFunction : MathFunction
    {
        public ExpFunction()
        {
            Title = "Exp";

            if (m_ParameterNames.Length == 0)
            {
                m_ParameterNames = new string[] { "f" };
            }
        }

        public override float Evaluate()
        {
            return Mathf.Exp(GetParameterValue(0));
        }
    }
}
