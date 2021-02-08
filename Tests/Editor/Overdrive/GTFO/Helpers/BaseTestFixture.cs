using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO
{
    public class BaseTestFixture
    {
        protected TestEditorWindow m_Window;

        protected TestEventHelpers EventHelper { get; private set; }

        [SetUp]
        public void SetUp()
        {
            m_Window = EditorWindow.GetWindow<TestEditorWindow>();

            // Make sure the window is shown (tests can fail if the window appears to low in the screen).
            m_Window.position = new Rect(Vector2.one * 10, Vector2.one * 1000);
            EventHelper = new TestEventHelpers(m_Window);
        }

        [TearDown]
        public void TearDown()
        {
            m_Window.Close();
        }
    }
}
