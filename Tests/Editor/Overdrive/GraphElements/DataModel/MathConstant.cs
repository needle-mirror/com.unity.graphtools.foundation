using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    public class MathConstant : MathNode
    {
        public float m_Value; // For property field (later)

        public void OnEnable()
        {
            name = "MathConstant";
        }

        public override void ResetConnections()
        {
        }

        public override float Evaluate()
        {
            return m_Value;
        }
    }
}
