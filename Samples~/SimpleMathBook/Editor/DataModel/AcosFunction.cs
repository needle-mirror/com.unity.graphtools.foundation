using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public class AcosFunction : MathFunction
    {
        public AcosFunction()
        {
            Title = "Acos";

            if (m_ParameterNames.Length == 0)
            {
                m_ParameterNames = new string[] { "f" };
            }
        }

        public override float Evaluate()
        {
            return Mathf.Acos(GetParameterValue(0));
        }
    }
}
