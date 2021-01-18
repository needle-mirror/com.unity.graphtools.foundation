using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels;
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
            m_Store = new Store(new TestState(default, m_GraphModel));
            StoreHelper.RegisterDefaultReducers(m_Store);
            m_GraphView = new TestGraphView(null, m_Store);
        }

        [TearDown]
        public void TearDown()
        {
            m_GraphModel = null;
            m_Store = null;
            m_GraphView = null;
        }

        [Test]
        public void PlacematHasExpectedParts()
        {
            var placematModel = m_GraphModel.CreatePlacemat();
            var placemat = new Placemat();
            placemat.SetupBuildAndUpdate(placematModel, m_Store, m_GraphView);

            Assert.IsNotNull(placemat.Q<VisualElement>(Placemat.titleContainerPartName));
            Assert.IsNotNull(placemat.Q<VisualElement>(Placemat.collapseButtonPartName));
            Assert.IsNotNull(placemat.Q<VisualElement>(Placemat.resizerPartName));
        }
    }
}
