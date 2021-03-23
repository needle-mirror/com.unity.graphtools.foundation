using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public class ClampFunction : MathFunction
    {
        public ClampFunction()
        {
            Title = "Clamp";

            if (m_ParameterNames.Length == 0)
            {
                m_ParameterNames = new string[] { "val", "min", "max" };
            }
        }

        public override float Evaluate()
        {
            return Mathf.Clamp(GetParameterValue(0), GetParameterValue(1), GetParameterValue(2));
        }
    }
}
