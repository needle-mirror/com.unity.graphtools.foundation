using System;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.Helpers;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIFromModelTests
{
    public class PlacematModelUpdateTests
    {
        GraphView m_GraphView;
        Store m_Store;
        GraphModel m_GraphModel;

        [SetUp]
        public void SetUp()
        {
            m_GraphModel = new GraphModel();
            m_Store = new Store(new Helpers.TestState(m_GraphModel), StoreHelper.RegisterReducers);
            m_GraphView = new TestGraphView(m_Store);
        }

        [Test]
        public void CollapsingPlacematModelCollapsesPlacemat()
        {
            var placematModel = new PlacematModel();
            var placemat = new Placemat();
            placemat.SetupBuildAndUpdate(placematModel, m_Store, m_GraphView);

            var collapseButton = placemat.Q<CollapseButton>(CollapsibleInOutNode.k_CollapseButtonPartName);
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

            var placematModel = new PlacematModel { Title = initialTitle };
            var placemat = new Placemat();
            placemat.SetupBuildAndUpdate(placematModel, m_Store, m_GraphView);

            var titleLabel = placemat.Q(EditableTitlePart.k_TitleLabelName).Q<Label>(EditableLabel.k_LabelName);
            Assert.AreEqual(initialTitle, titleLabel.text);

            placematModel.Title = newTitle;
            placemat.UpdateFromModel();
            Assert.AreEqual(newTitle, titleLabel.text);
        }

        [Test]
        public void ResizingPlacematModelUpdatesPlacematRect()
        {
            var initialRect = new Rect(0, 0, 400, 400);
            var newRect = new Rect(50, 70, 500, 300);;

            var placematModel = new PlacematModel { PositionAndSize = initialRect };
            var placemat = new Placemat();
            placemat.SetupBuildAndUpdate(placematModel, m_Store, m_GraphView);

            Assert.AreEqual(initialRect, new Rect(placemat.style.left.value.value, placemat.style.top.value.value, placemat.style.width.value.value, placemat.style.height.value.value));

            placematModel.PositionAndSize = newRect;
            placemat.UpdateFromModel();
            Assert.AreEqual(newRect, new Rect(placemat.style.left.value.value, placemat.style.top.value.value, placemat.style.width.value.value, placemat.style.height.value.value));
        }
    }
}
