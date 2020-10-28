using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    public class PIConstant : MathConstant
    {
        public new void OnEnable()
        {
            name = "PI";
            m_Value = Mathf.PI;
        }
    }
}
