using System.Collections;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.Helpers;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIFromModelTests
{
    class NodeActionTests : BaseTestFixture
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
        public IEnumerator CollapsibleNodeCollapsesOnValueChange()
        {
            var nodeModel = new IONodeModel();
            var node = new CollapsibleInOutNode();
            node.SetupBuildAndUpdate(nodeModel, m_Store, m_GraphView);
            m_GraphView.AddElement(node);
            yield return null;

            var collapseButton = node.Q<CollapseButton>(CollapsibleInOutNode.k_CollapseButtonPartName);
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
            var nodeModel = new IONodeModel();
            var node = new CollapsibleInOutNode();
            node.SetupBuildAndUpdate(nodeModel, m_Store, m_GraphView);
            m_GraphView.AddElement(node);
            yield return null;

            var collapseButton = node.Q<CollapseButton>(CollapsibleInOutNode.k_CollapseButtonPartName);
            Assert.IsNotNull(collapseButton);
            Assert.IsFalse(collapseButton.value);
            Assert.IsFalse(nodeModel.Collapsed);

            var collapseButtonIcon = node.Q<CollapseButton>(CollapsibleInOutNode.k_CollapseButtonPartName).Q(CollapseButton.k_IconElementName);
            var clickPosition = collapseButton.parent.LocalToWorld(collapseButton.layout.center);
            Click(collapseButton, clickPosition);
            yield return null;

            Assert.IsTrue(nodeModel.Collapsed);
        }

        [UnityTest]
        public IEnumerator RenameNodeRenamesModel()
        {
            const string newName = "New Name";

            var nodeModel = new NodeModel { Title = "Node" };
            var node = new Node();
            node.SetupBuildAndUpdate(nodeModel, m_Store, m_GraphView);
            m_GraphView.AddElement(node);
            yield return null;

            var label = node.Q(Node.k_TitleContainerPartName).Q(EditableLabel.k_LabelName);
            var clickPosition = label.parent.LocalToWorld(label.layout.center);
            DoubleClick(label, clickPosition);

            Type(label, newName);

            Click(m_GraphView, m_GraphView.layout.min);
            yield return null;

            Assert.AreEqual(newName, nodeModel.Title);
        }
    }
}
