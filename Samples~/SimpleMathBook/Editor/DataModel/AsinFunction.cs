using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public class AsinFunction : MathFunction
    {
        public AsinFunction()
        {
            Title = "Asin";

            if (m_ParameterNames.Length == 0)
            {
                m_ParameterNames = new string[] { "f" };
            }
        }

        public override float Evaluate()
        {
            return Mathf.Asin(GetParameterValue(0));
        }
    }
}
