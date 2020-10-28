using System.Collections;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIFromModelTests
{
    public class StickyNoteActionTests : BaseTestFixture
    {
        GraphView m_GraphView;
        Store m_Store;
        GraphModel m_GraphModel;

        [SetUp]
        public new void SetUp()
        {
            m_GraphModel = new GraphModel();
            m_Store = new Store(new TestState(m_GraphModel));
            StoreHelper.RegisterDefaultReducers(m_Store);
            m_GraphView = new TestGraphView(m_Window, m_Store);

            m_GraphView.name = "theView";
            m_GraphView.viewDataKey = "theView";
            m_GraphView.StretchToParentSize();

            m_Window.rootVisualElement.Add(m_GraphView);
        }

        [UnityTest]
        public IEnumerator RenameStickyNoteRenamesModel()
        {
            const string newName = "New Name";

            var stickyNoteModel = m_GraphModel.CreateStickyNote();
            stickyNoteModel.Title = "My Note";
            var stickyNote = new StickyNote();
            stickyNote.SetupBuildAndUpdate(stickyNoteModel, m_Store, m_GraphView);
            m_GraphView.AddElement(stickyNote);
            yield return null;

            var label = stickyNote.Q(StickyNote.k_TitleContainerPartName).Q(EditableLabel.k_LabelName);
            var clickPosition = label.parent.LocalToWorld(label.layout.center);
            DoubleClick(label, clickPosition);

            Type(label, newName);

            Click(m_GraphView, m_GraphView.layout.min);
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
            stickyNote.SetupBuildAndUpdate(stickyNoteModel, m_Store, m_GraphView);
            m_GraphView.AddElement(stickyNote);
            yield return null;

            var label = stickyNote.Q(StickyNote.k_ContentContainerPartName).Q(EditableLabel.k_LabelName);
            var clickPosition = label.parent.LocalToWorld(label.layout.center);
            DoubleClick(label, clickPosition);

            Type(label, newContent);

            Click(m_GraphView, m_GraphView.layout.min);
            yield return null;

            Assert.AreEqual(newContent, stickyNoteModel.Contents);
        }

        [UnityTest]
        public IEnumerator ResizeStickyNoteChangeModelRect()
        {
            var originalRect = new Rect(0, 0, 400, 400);
            var move = new Vector2(100, 0);

            var stickyNoteModel = m_GraphModel.CreateStickyNote(originalRect);
            stickyNoteModel.Title = "Placemat";
            stickyNoteModel.PositionAndSize = originalRect;
            var stickyNote = new StickyNote();
            stickyNote.SetupBuildAndUpdate(stickyNoteModel, m_Store, m_GraphView);
            m_GraphView.AddElement(stickyNote);
            yield return null;

            var rightResizer = stickyNote.Q(Placemat.k_ResizerPartName).Q("right-resize");
            var clickPosition = rightResizer.parent.LocalToWorld(rightResizer.layout.center);
            ClickDragRelease(rightResizer, clickPosition, move);
            yield return null;

            var newRect = new Rect(originalRect.position, originalRect.size + move);
            Assert.AreEqual(newRect, stickyNoteModel.PositionAndSize);
        }
    }
}
