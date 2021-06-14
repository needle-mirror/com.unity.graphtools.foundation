using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public class MaxFunction : MathFunction
    {
        public override string Title
        {
            get => "Max";
            set { }
        }

        public MaxFunction()
        {
            if (m_ParameterNames.Length == 0)
            {
                m_ParameterNames = new[] { "a", "b" };
            }
        }

        public override float Evaluate()
        {
            return Mathf.Max(GetParameterValue(0), GetParameterValue(1));
        }
    }
}
