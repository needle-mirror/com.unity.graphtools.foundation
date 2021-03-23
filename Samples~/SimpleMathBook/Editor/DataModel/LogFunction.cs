using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public class LogFunction : MathFunction
    {
        public LogFunction()
        {
            Title = "Log";

            if (m_ParameterNames.Length == 0)
            {
                m_ParameterNames = new string[] { "f", "p" };
            }
        }

        public override float Evaluate()
        {
            return Mathf.Log(GetParameterValue(0), GetParameterValue(1));
        }
    }
}
