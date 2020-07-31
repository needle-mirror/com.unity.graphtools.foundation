using System;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements.Utilities
{
    public class TestGraphElement : GraphElement
    {
        Label m_Text;

        public TestGraphElement()
        {
            m_Text = new Label();
            Add(m_Text);
        }

        public override bool IsResizable()
        {
            return true;
        }
    }
}
