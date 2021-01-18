using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIFromModelTests
{
    public class StickyNoteUICreationTests
    {
        GraphView m_GraphView;
        Store m_Store;
        GraphModel m_GraphModel;

        [SetUp]
        public void SetUp()
        {
            m_GraphModel = new GraphModel();
            m_Store = new Store(new TestState(default, m_GraphModel));
            StoreHelper.RegisterDefaultReducers(m_Store);
            m_GraphView = new TestGraphView(null, m_Store);
        }

        [TearDown]
        public void TearDown()
        {
            m_GraphModel = null;
            m_Store = null;
            m_GraphView = null;
        }

        [Test]
        public void StickyNoteHasExpectedParts()
        {
            var stickyNoteModel = m_GraphModel.CreateStickyNote();
            var stickyNote = new StickyNote();
            stickyNote.SetupBuildAndUpdate(stickyNoteModel, m_Store, m_GraphView);

            Assert.IsNotNull(stickyNote.Q<VisualElement>(StickyNote.titleContainerPartName));
            Assert.IsNotNull(stickyNote.Q<VisualElement>(StickyNote.contentContainerPartName));
            Assert.IsNotNull(stickyNote.Q<VisualElement>(StickyNote.resizerPartName));
        }
    }
}
