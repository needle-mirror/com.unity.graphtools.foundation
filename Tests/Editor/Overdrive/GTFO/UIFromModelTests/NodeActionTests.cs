using System.Collections;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using GraphModel = UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels.GraphModel;
using NodeModel = UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels.NodeModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIFromModelTests
{
    class NodeActionTests : BaseTestFixture
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
        public IEnumerator CollapsibleNodeCollapsesOnValueChange()
        {
            var nodeModel = m_GraphModel.CreateNode<IONodeModel>();
            var node = new CollapsibleInOutNode();
            node.SetupBuildAndUpdate(nodeModel, m_Store, m_GraphView);
            m_GraphView.AddElement(node);
            yield return null;

            var collapseButton = node.Q<CollapseButton>(CollapsibleInOutNode.collapseButtonPartName);
            Assert.IsNotNull(collapseButton);
            Assert.IsFalse(collapseButton.value);
            Assert.IsFalse(nodeModel.Collapsed);

            collapseButton.value = true;
            yield return null;

            Assert.IsTrue(nodeModel.Collapsed);
        }

        [UnityTest]
        public IEnumerator CollapsibleNodeCollapsesOnClick()
        {
            var nodeModel = m_GraphModel.CreateNode<IONodeModel>();
            var node = new CollapsibleInOutNode();
            node.SetupBuildAndUpdate(nodeModel, m_Store, m_GraphView);
            m_GraphView.AddElement(node);
            yield return null;

            var collapseButton = node.Q<CollapseButton>(CollapsibleInOutNode.collapseButtonPartName);
            Assert.IsNotNull(collapseButton);
            Assert.IsFalse(collapseButton.value);
            Assert.IsFalse(nodeModel.Collapsed);

            var collapseButtonIcon = node.Q<CollapseButton>(CollapsibleInOutNode.collapseButtonPartName).Q(CollapseButton.iconElementName);
            var clickPosition = collapseButton.parent.LocalToWorld(collapseButton.layout.center);
            Click(collapseButton, clickPosition);
            yield return null;

            Assert.IsTrue(nodeModel.Collapsed);
        }

        [UnityTest]
        public IEnumerator RenameNodeRenamesModel()
        {
            const string newName = "New Name";

            var nodeModel = m_GraphModel.CreateNode<NodeModel>("Node");
            var node = new Node();
            node.SetupBuildAndUpdate(nodeModel, m_Store, m_GraphView);
            m_GraphView.AddElement(node);
            yield return null;

            var label = node.Q(Node.titleContainerPartName).Q(EditableLabel.labelName);
            var clickPosition = label.parent.LocalToWorld(label.layout.center);
            DoubleClick(label, clickPosition);

            Type(label, newName);

            Click(m_GraphView, m_GraphView.layout.min);
            yield return null;

            Assert.AreEqual(newName, nodeModel.Title);
        }
    }
}
