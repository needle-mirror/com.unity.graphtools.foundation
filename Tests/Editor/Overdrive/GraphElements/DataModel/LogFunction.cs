using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    public class LogFunction : MathFunction
    {
        void OnEnable()
        {
            name = "Log";
            if (m_ParameterIDs.Length == 0)
            {
                m_ParameterIDs = new MathNodeID[2];
            }

            if (m_ParameterNames.Length == 0)
            {
                m_ParameterNames = new string[] { "f", "p" };
            }
        }

        public override float Evaluate()
        {
            float input = 0.0f;
            if (GetParameter(0) != null)
            {
                input =  GetParameter(0).Evaluate();
            }
            float power = 0.0f;
            if (GetParameter(1) != null)
            {
                power =  GetParameter(1).Evaluate();
            }
            return Mathf.Log(input, power);
        }
    }
}
