using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIFromModelTests
{
    public class StickyNoteModelUpdateTests
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
        public void RenamingStickyNoteModelUpdatesTitleLabel()
        {
            const string initialTitle = "Initial title";
            const string newTitle = "New title";

            var stickyNoteModel = m_GraphModel.CreateStickyNote(Rect.zero);
            stickyNoteModel.Title = initialTitle;
            var stickyNote = new StickyNote();
            stickyNote.SetupBuildAndUpdate(stickyNoteModel, m_CommandDispatcher, m_GraphView);

            var titleLabel = stickyNote.SafeQ(StickyNote.titleContainerPartName).SafeQ<Label>(EditableLabel.labelName);
            Assert.AreEqual(initialTitle, titleLabel.text);

            stickyNoteModel.Title = newTitle;
            stickyNote.UpdateFromModel();
            Assert.AreEqual(newTitle, titleLabel.text);
        }

        [Test]
        public void ChangingContentOfStickyNoteModelUpdatesContentLabel()
        {
            const string initialContent = "Initial content";
            const string newContent = "New content";

            var stickyNoteModel = m_GraphModel.CreateStickyNote(Rect.zero);
            stickyNoteModel.Contents = initialContent;
            var stickyNote = new StickyNote();
            stickyNote.SetupBuildAndUpdate(stickyNoteModel, m_CommandDispatcher, m_GraphView);

            var contentLabel = stickyNote.SafeQ(StickyNote.contentContainerPartName).SafeQ<Label>(EditableLabel.labelName);
            Assert.AreEqual(initialContent, contentLabel.text);

            stickyNoteModel.Contents = newContent;
            stickyNote.UpdateFromModel();
            Assert.AreEqual(newContent, contentLabel.text);
        }

        [Test]
        public void ResizingStickyNoteModelUpdatesStickyNoteRect()
        {
            var initialRect = new Rect(0, 0, 400, 400);
            var newRect = new Rect(50, 70, 500, 300);

            var stickyNoteModel = m_GraphModel.CreateStickyNote(Rect.zero);
            stickyNoteModel.PositionAndSize = initialRect;
            var stickyNote = new StickyNote();
            stickyNote.SetupBuildAndUpdate(stickyNoteModel, m_CommandDispatcher, m_GraphView);

            Assert.AreEqual(initialRect, new Rect(stickyNote.style.left.value.value, stickyNote.style.top.value.value, stickyNote.style.width.value.value, stickyNote.style.height.value.value));

            stickyNoteModel.PositionAndSize = newRect;
            stickyNote.UpdateFromModel();
            Assert.AreEqual(newRect, new Rect(stickyNote.style.left.value.value, stickyNote.style.top.value.value, stickyNote.style.width.value.value, stickyNote.style.height.value.value));
        }
    }
}
