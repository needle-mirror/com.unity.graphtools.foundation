using System.Collections;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.Helpers;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIFromModelTests
{
    public class PlacematActionTests : BaseTestFixture
    {
        GraphView m_GraphView;
        Helpers.TestStore m_Store;
        GraphModel m_GraphModel;

        [SetUp]
        public new void SetUp()
        {
            m_GraphModel = new GraphModel();
            m_Store = new Helpers.TestStore(new Helpers.TestState(m_GraphModel));
            m_GraphView = new TestGraphView(m_Store);

            m_GraphView.name = "theView";
            m_GraphView.viewDataKey = "theView";
            m_GraphView.StretchToParentSize();

            m_Window.rootVisualElement.Add(m_GraphView);
        }

        [UnityTest]
        public IEnumerator PlacematCollapsesOnValueChanged()
        {
            var placematModel = new PlacematModel();
            var placemat = new Placemat();
            placemat.SetupBuildAndUpdate(placematModel, m_Store, m_GraphView);
            yield return null;

            var collapseButton = placemat.Q<CollapseButton>(Placemat.k_CollapseButtonPartName);
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
            var placematModel = new PlacematModel();
            var placemat = new Placemat();
            placemat.SetupBuildAndUpdate(placematModel, m_Store, m_GraphView);
            yield return null;

            var collapseButton = placemat.Q<CollapseButton>(Placemat.k_CollapseButtonPartName);
            Assert.IsNotNull(collapseButton);
            Assert.IsFalse(collapseButton.value);
            Assert.IsFalse(placematModel.Collapsed);

            var collapseButtonIcon = placemat.Q<CollapseButton>(Placemat.k_CollapseButtonPartName).Q(CollapseButton.k_IconElementName);
            var clickPosition = collapseButton.parent.LocalToWorld(collapseButton.layout.center);
            Click(collapseButton, clickPosition);
            yield return null;

            Assert.IsTrue(placematModel.Collapsed);
        }

        [UnityTest]
        public IEnumerator RenamePlacematRenamesModel()
        {
            const string newName = "New Name";

            var placematModel = new PlacematModel { Title = "Placemat" };
            var placemat = new Placemat();
            placemat.SetupBuildAndUpdate(placematModel, m_Store, m_GraphView);
            yield return null;

            var label = placemat.Q(Placemat.k_TitleContainerPartName).Q(EditableLabel.k_LabelName);
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

            var placematModel = new PlacematModel { Title = "Placemat", PositionAndSize = originalRect};
            var placemat = new Placemat();
            placemat.SetupBuildAndUpdate(placematModel, m_Store, m_GraphView);
            yield return null;

            var rightResizer = placemat.Q(Placemat.k_ResizerPartName).Q("right-resize");
            var clickPosition = rightResizer.parent.LocalToWorld(rightResizer.layout.center);
            ClickDragRelease(rightResizer, clickPosition, move);
            yield return null;

            var newRect = new Rect(originalRect.position, originalRect.size + move);
            Assert.AreEqual(newRect, placematModel.PositionAndSize);
        }
    }
}
