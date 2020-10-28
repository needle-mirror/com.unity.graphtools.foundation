using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    public class MaxFunction : MathFunction
    {
        void OnEnable()
        {
            name = "Max";
            if (m_ParameterIDs.Length == 0)
            {
                m_ParameterIDs = new MathNodeID[2];
            }

            if (m_ParameterNames.Length == 0)
            {
                m_ParameterNames = new string[] { "a", "b" };
            }
        }

        public override float Evaluate()
        {
            float left = 0.0f;
            float right = 0.0f;
            if (GetParameter(0) != null)
            {
                left =  GetParameter(0).Evaluate();
            }
            if (GetParameter(1) != null)
            {
                right =  GetParameter(1).Evaluate();
            }
            return Mathf.Max(left, right);
        }
    }
}
