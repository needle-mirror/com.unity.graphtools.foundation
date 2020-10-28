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
            m_Store = new Store(new TestState(m_GraphModel));
            StoreHelper.RegisterDefaultReducers(m_Store);
            m_GraphView = new TestGraphView(null, m_Store);
        }

        [Test]
        public void StickyNoteHasExpectedParts()
        {
            var stickyNoteModel = m_GraphModel.CreateStickyNote();
            var stickyNote = new StickyNote();
            stickyNote.SetupBuildAndUpdate(stickyNoteModel, m_Store, m_GraphView);

            Assert.IsNotNull(stickyNote.Q<VisualElement>(StickyNote.k_TitleContainerPartName));
            Assert.IsNotNull(stickyNote.Q<VisualElement>(StickyNote.k_ContentContainerPartName));
            Assert.IsNotNull(stickyNote.Q<VisualElement>(StickyNote.k_ResizerPartName));
        }
    }
}
