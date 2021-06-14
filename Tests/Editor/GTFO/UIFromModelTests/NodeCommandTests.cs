using System.Collections;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using NodeModel = UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels.NodeModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIFromModelTests
{
    class NodeCommandTests : BaseTestFixture
    {
        [UnityTest]
        public IEnumerator CollapsibleNodeCollapsesOnValueChange()
        {
            var nodeModel = GraphModel.CreateNode<IONodeModel>();
            MarkGraphViewStateDirty();
            yield return null;

            var node = nodeModel.GetUI<GraphElement>(GraphView);
            var collapseButton = node.SafeQ<CollapseButton>(CollapsibleInOutNode.collapseButtonPartName);
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
            var nodeModel = GraphModel.CreateNode<IONodeModel>();
            // Add a port to make the node collapsible.
            nodeModel.AddInputPort("port", PortType.Data, TypeHandle.Void, null, PortOrientation.Horizontal, PortModelOptions.NoEmbeddedConstant);
            MarkGraphViewStateDirty();
            yield return null;

            var node = nodeModel.GetUI<GraphElement>(GraphView);
            var collapseButton = node.SafeQ<CollapseButton>(CollapsibleInOutNode.collapseButtonPartName);
            Assert.IsNotNull(collapseButton);
            Assert.IsFalse(collapseButton.value);
            Assert.IsFalse(nodeModel.Collapsed);

            var clickPosition = collapseButton.parent.LocalToWorld(collapseButton.layout.center);

            // Move the mouse over to make the button appear.
            EventHelper.MouseMoveEvent(new Vector2(0, 0), clickPosition);
            EventHelper.MouseMoveEvent(clickPosition, clickPosition + Vector2.down);

            EventHelper.Click(clickPosition);
            yield return null;

            Assert.IsTrue(nodeModel.Collapsed);
        }

        [UnityTest]
        public IEnumerator RenameNodeRenamesModel()
        {
            const string newName = "New Name";

            var nodeModel = GraphModel.CreateNode<NodeModel>("Node");
            MarkGraphViewStateDirty();
            yield return null;

            var node = nodeModel.GetUI<GraphElement>(GraphView);
            var label = node.SafeQ(CollapsibleInOutNode.titleIconContainerPartName).SafeQ(EditableLabel.labelName);
            var clickPosition = label.parent.LocalToWorld(label.layout.center);
            EventHelper.Click(clickPosition, clickCount: 2);

            EventHelper.Type(newName);

            EventHelper.Click(GraphView.layout.min);
            yield return null;

            Assert.AreEqual(newName, nodeModel.Title);
        }
    }
}
