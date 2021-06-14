using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO
{
    public class BaseTestFixture
    {
        protected TestEditorWindow m_Window;

        protected GraphModel GraphModel => m_Window.GraphModel as GraphModel;
        protected CommandDispatcher CommandDispatcher => m_Window.CommandDispatcher;
        protected GraphView GraphView => m_Window.GraphView;

        protected TestEventHelpers EventHelper { get; private set; }

        [SetUp]
        public void SetUp()
        {
            m_Window = EditorWindow.GetWindowWithRect<TestEditorWindow>(new Rect(100, 100, 800, 800));
            EventHelper = new TestEventHelpers(m_Window);
        }

        [TearDown]
        public void TearDown()
        {
            m_Window.Close();
        }

        protected void MarkGraphViewStateDirty()
        {
            using (var updater = CommandDispatcher.State.GraphViewState.UpdateScope)
            {
                updater.ForceCompleteUpdate();
            }
        }
    }
}
