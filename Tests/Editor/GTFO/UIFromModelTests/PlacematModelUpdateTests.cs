using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIFromModelTests
{
    public class PlacematModelUpdateTests
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
        public void CollapsingPlacematModelCollapsesPlacemat()
        {
            var placematModel = m_GraphModel.CreatePlacemat(Rect.zero);
            var placemat = new Placemat();
            placemat.SetupBuildAndUpdate(placematModel, m_CommandDispatcher, m_GraphView);

            var collapseButton = placemat.SafeQ<CollapseButton>(CollapsibleInOutNode.collapseButtonPartName);
            Assert.IsFalse(collapseButton.value);

            placematModel.Collapsed = true;
            placemat.UpdateFromModel();
            Assert.IsTrue(collapseButton.value);
        }

        [Test]
        public void RenamingPlacematModelUpdatesTitleLabel()
        {
            const string initialTitle = "Initial title";
            const string newTitle = "New title";

            var placematModel = m_GraphModel.CreatePlacemat(Rect.zero);
            placematModel.Title = initialTitle;
            var placemat = new Placemat();
            placemat.SetupBuildAndUpdate(placematModel, m_CommandDispatcher, m_GraphView);

            var titleLabel = placemat.SafeQ(EditableTitlePart.titleLabelName).SafeQ<Label>(EditableLabel.labelName);
            Assert.AreEqual(initialTitle, titleLabel.text);

            placematModel.Title = newTitle;
            placemat.UpdateFromModel();
            Assert.AreEqual(newTitle, titleLabel.text);
        }

        [Test]
        public void ResizingPlacematModelUpdatesPlacematRect()
        {
            var initialRect = new Rect(0, 0, 400, 400);
            var newRect = new Rect(50, 70, 500, 300);

            var placematModel = m_GraphModel.CreatePlacemat(Rect.zero);
            placematModel.PositionAndSize = initialRect;
            var placemat = new Placemat();
            placemat.SetupBuildAndUpdate(placematModel, m_CommandDispatcher, m_GraphView);

            Assert.AreEqual(initialRect, new Rect(placemat.style.left.value.value, placemat.style.top.value.value, placemat.style.width.value.value, placemat.style.height.value.value));

            placematModel.PositionAndSize = newRect;
            placemat.UpdateFromModel();
            Assert.AreEqual(newRect, new Rect(placemat.style.left.value.value, placemat.style.top.value.value, placemat.style.width.value.value, placemat.style.height.value.value));
        }
    }
}
