using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public class CosFunction : MathFunction
    {
        public CosFunction()
        {
            Title = "Cos";

            if (m_ParameterNames.Length == 0)
            {
                m_ParameterNames = new string[] { "f" };
            }
        }

        public override float Evaluate()
        {
            return Mathf.Cos(GetParameterValue(0));
        }
    }
}
