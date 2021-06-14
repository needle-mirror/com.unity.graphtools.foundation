using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public class AcosFunction : MathFunction
    {
        public override string Title
        {
            get => "Acos";
            set { }
        }

        public AcosFunction()
        {
            if (m_ParameterNames.Length == 0)
            {
                m_ParameterNames = new[] { "f" };
            }
        }

        public override float Evaluate()
        {
            return Mathf.Acos(GetParameterValue(0));
        }
    }
}
