using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIFromModelTests
{
    public class StickyNoteUICreationTests
    {
        GraphView m_GraphView;
        CommandDispatcher m_CommandDispatcher;
        GraphModel m_GraphModel;

        [SetUp]
        public void SetUp()
        {
            m_GraphModel = new GraphModel();
            m_CommandDispatcher = new CommandDispatcher(new TestGraphToolState(default, m_GraphModel));
            m_GraphView = new GraphView(null, m_CommandDispatcher, "");
        }

        [TearDown]
        public void TearDown()
        {
            m_GraphModel = null;
            m_CommandDispatcher = null;
            m_GraphView = null;
        }

        [Test]
        public void StickyNoteHasExpectedParts()
        {
            var stickyNoteModel = m_GraphModel.CreateStickyNote(Rect.zero);
            var stickyNote = new StickyNote();
            stickyNote.SetupBuildAndUpdate(stickyNoteModel, m_CommandDispatcher, m_GraphView);

            Assert.IsNotNull(stickyNote.SafeQ<VisualElement>(StickyNote.titleContainerPartName));
            Assert.IsNotNull(stickyNote.SafeQ<VisualElement>(StickyNote.contentContainerPartName));
            Assert.IsNotNull(stickyNote.SafeQ<VisualElement>(StickyNote.resizerPartName));
        }
    }
}
