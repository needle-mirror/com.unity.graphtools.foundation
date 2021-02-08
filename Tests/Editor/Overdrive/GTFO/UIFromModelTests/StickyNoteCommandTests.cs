using System.Collections;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIFromModelTests
{
    public class StickyNoteCommandTests : BaseTestFixture
    {
        GraphView m_GraphView;
        CommandDispatcher m_CommandDispatcher;
        GraphModel m_GraphModel;

        [SetUp]
        public new void SetUp()
        {
            m_GraphModel = new GraphModel();
            m_CommandDispatcher = new CommandDispatcher(new TestGraphToolState(m_Window.GUID, m_GraphModel));
            CommandDispatcherHelper.RegisterDefaultCommandHandlers(m_CommandDispatcher);
            m_GraphView = new GraphView(m_Window, m_CommandDispatcher);

            m_GraphView.name = "theView";
            m_GraphView.viewDataKey = "theView";
            m_GraphView.StretchToParentSize();

            m_Window.rootVisualElement.Add(m_GraphView);
        }

        [TearDown]
        public new void TearDown()
        {
            m_Window.rootVisualElement.Remove(m_GraphView);
            m_GraphModel = null;
            m_CommandDispatcher = null;
            m_GraphView = null;
        }

        [UnityTest]
        public IEnumerator RenameStickyNoteRenamesModel()
        {
            const string newName = "New Name";

            var stickyNoteModel = m_GraphModel.CreateStickyNote();
            stickyNoteModel.Title = "My Note";
            var stickyNote = new StickyNote();
            stickyNote.SetupBuildAndUpdate(stickyNoteModel, m_CommandDispatcher, m_GraphView);
            m_GraphView.AddElement(stickyNote);
            yield return null;

            var label = stickyNote.Q(StickyNote.titleContainerPartName).Q(EditableLabel.labelName);
            var clickPosition = label.parent.LocalToWorld(label.layout.center);
            EventHelper.Click(clickPosition, clickCount: 2);

            EventHelper.Type(newName);

            // Commit the changes by clicking outside the field.
            EventHelper.Click(m_GraphView.layout.min);

            yield return null;

            Assert.AreEqual(newName, stickyNoteModel.Title);
        }

        [UnityTest]
        public IEnumerator ChangeStickyNoteContentUpdatesModel()
        {
            const string newContent = "New Content";

            var stickyNoteModel = m_GraphModel.CreateStickyNote(new Rect(0, 0, 200, 200));
            stickyNoteModel.Title = "My Note";
            stickyNoteModel.Contents = "Old Content";
            var stickyNote = new StickyNote();
            stickyNote.SetupBuildAndUpdate(stickyNoteModel, m_CommandDispatcher, m_GraphView);
            m_GraphView.AddElement(stickyNote);
            yield return null;

            var label = stickyNote.Q(StickyNote.contentContainerPartName).Q(EditableLabel.labelName);
            var clickPosition = label.parent.LocalToWorld(label.layout.center);
            EventHelper.Click(clickPosition, clickCount: 2);

            EventHelper.Type(newContent);

            // Commit the changes by clicking outside the field.
            EventHelper.Click(m_GraphView.layout.min);

            yield return null;

            Assert.AreEqual(newContent, stickyNoteModel.Contents);
        }

        [UnityTest]
        public IEnumerator ResizeStickyNoteChangeModelRect()
        {
            var originalRect = new Rect(0, 0, 100, 100);
            var move = new Vector2(100, 0);

            var stickyNoteModel = m_GraphModel.CreateStickyNote(originalRect);
            stickyNoteModel.Title = "Placemat";
            stickyNoteModel.PositionAndSize = originalRect;
            var stickyNote = new StickyNote();
            stickyNote.SetupBuildAndUpdate(stickyNoteModel, m_CommandDispatcher, m_GraphView);
            m_GraphView.AddElement(stickyNote);
            yield return null;

            var rightResizer = stickyNote.Q(Placemat.resizerPartName).Q("right-resize");
            var clickPosition = rightResizer.parent.LocalToWorld(rightResizer.layout.center);
            EventHelper.DragTo(clickPosition, clickPosition + move);
            yield return null;

            var newRect = new Rect(originalRect.position, originalRect.size + move);
            Assert.AreEqual(newRect, stickyNoteModel.PositionAndSize);
        }
    }
}
