using System;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.Helpers;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIFromModelTests
{
    public class PlacematUICreationTests
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
        public void PlacematHasExpectedParts()
        {
            var placematModel = new PlacematModel();
            var placemat = new Placemat();
            placemat.SetupBuildAndUpdate(placematModel, m_Store, m_GraphView);

            Assert.IsNotNull(placemat.Q<VisualElement>(Placemat.k_TitleContainerPartName));
            Assert.IsNotNull(placemat.Q<VisualElement>(Placemat.k_CollapseButtonPartName));
            Assert.IsNotNull(placemat.Q<VisualElement>(Placemat.k_ResizerPartName));
        }
    }
}
