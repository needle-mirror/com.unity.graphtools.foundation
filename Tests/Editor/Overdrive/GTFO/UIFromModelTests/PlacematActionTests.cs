using System.Collections;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIFromModelTests
{
    public class PlacematActionTests : BaseTestFixture
    {
        GraphView m_GraphView;
        Store m_Store;
        GraphModel m_GraphModel;

        [SetUp]
        public new void SetUp()
        {
            m_GraphModel = new GraphModel();
            m_Store = new Store(new TestState(m_Window.GUID, m_GraphModel));
            StoreHelper.RegisterDefaultReducers(m_Store);
            m_GraphView = new TestGraphView(m_Window, m_Store);

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
            m_Store = null;
            m_GraphView = null;
        }

        [UnityTest]
        public IEnumerator PlacematCollapsesOnValueChanged()
        {
            var placematModel = m_GraphModel.CreatePlacemat();
            var placemat = new Placemat();
            placemat.SetupBuildAndUpdate(placematModel, m_Store, m_GraphView);
            yield return null;

            var collapseButton = placemat.Q<CollapseButton>(Placemat.collapseButtonPartName);
            Assert.IsNotNull(collapseButton);
            Assert.IsFalse(collapseButton.value);
            Assert.IsFalse(placematModel.Collapsed);

            collapseButton.value = true;
            yield return null;

            Assert.IsTrue(placematModel.Collapsed);
        }

        [UnityTest]
        public IEnumerator PlacematCollapsesOnClick()
        {
            var placematModel = m_GraphModel.CreatePlacemat();
            var placemat = new Placemat();
            placemat.SetupBuildAndUpdate(placematModel, m_Store, m_GraphView);
            yield return null;

            var collapseButton = placemat.Q<CollapseButton>(Placemat.collapseButtonPartName);
            Assert.IsNotNull(collapseButton);
            Assert.IsFalse(collapseButton.value);
            Assert.IsFalse(placematModel.Collapsed);

            var collapseButtonIcon = placemat.Q<CollapseButton>(Placemat.collapseButtonPartName).Q(CollapseButton.iconElementName);
            var clickPosition = collapseButton.parent.LocalToWorld(collapseButton.layout.center);
            Click(collapseButton, clickPosition);
            yield return null;

            Assert.IsTrue(placematModel.Collapsed);
        }

        [UnityTest]
        public IEnumerator RenamePlacematRenamesModel()
        {
            const string newName = "New Name";

            var placematModel = m_GraphModel.CreatePlacemat();
            placematModel.Title = "Placemat";
            var placemat = new Placemat();
            placemat.SetupBuildAndUpdate(placematModel, m_Store, m_GraphView);
            yield return null;

            var label = placemat.Q(Placemat.titleContainerPartName).Q(EditableLabel.labelName);
            var clickPosition = label.parent.LocalToWorld(label.layout.center);
            DoubleClick(label, clickPosition);

            Type(label, newName);

            Click(m_GraphView, m_GraphView.layout.min);
            yield return null;

            Assert.AreEqual(newName, placematModel.Title);
        }

        [UnityTest]
        public IEnumerator ResizePlacematChangeModelRect()
        {
            var originalRect = new Rect(0, 0, 400, 400);
            var move = new Vector2(100, 0);

            var placematModel = m_GraphModel.CreatePlacemat(originalRect);
            placematModel.Title = "Placemat";
            var placemat = new Placemat();
            placemat.SetupBuildAndUpdate(placematModel, m_Store, m_GraphView);
            yield return null;

            var rightResizer = placemat.Q(Placemat.resizerPartName).Q("right-resize");
            var clickPosition = rightResizer.parent.LocalToWorld(rightResizer.layout.center);
            ClickDragRelease(rightResizer, clickPosition, move);
            yield return null;

            var newRect = new Rect(originalRect.position, originalRect.size + move);
            Assert.AreEqual(newRect, placematModel.PositionAndSize);
        }
    }
}
