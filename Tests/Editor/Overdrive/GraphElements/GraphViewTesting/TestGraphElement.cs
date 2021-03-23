using System;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    public class TestGraphElement : GraphElement
    {
        Label m_Text;

        public TestGraphElement()
        {
            m_Text = new Label();
            Add(m_Text);
        }
    }
}
