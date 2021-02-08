using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIFromModelTests
{
    public class PlacematUICreationTests
    {
        GraphView m_GraphView;
        CommandDispatcher m_CommandDispatcher;
        GraphModel m_GraphModel;

        [SetUp]
        public void SetUp()
        {
            m_GraphModel = new GraphModel();
            m_CommandDispatcher = new CommandDispatcher(new TestGraphToolState(default, m_GraphModel));
            CommandDispatcherHelper.RegisterDefaultCommandHandlers(m_CommandDispatcher);
            m_GraphView = new GraphView(null, m_CommandDispatcher);
        }

        [TearDown]
        public void TearDown()
        {
            m_GraphModel = null;
            m_CommandDispatcher = null;
            m_GraphView = null;
        }

        [Test]
        public void PlacematHasExpectedParts()
        {
            var placematModel = m_GraphModel.CreatePlacemat();
            var placemat = new Placemat();
            placemat.SetupBuildAndUpdate(placematModel, m_CommandDispatcher, m_GraphView);

            Assert.IsNotNull(placemat.Q<VisualElement>(Placemat.titleContainerPartName));
            Assert.IsNotNull(placemat.Q<VisualElement>(Placemat.collapseButtonPartName));
            Assert.IsNotNull(placemat.Q<VisualElement>(Placemat.resizerPartName));
        }
    }
}
