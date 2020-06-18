using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine.UIElements;
using UnityEngine.TestTools;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements.Utilities;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    public class SnapToPortTests : GraphViewTester
    {
        const float k_SnappingDistance = 8.0f;

        static readonly Vector2 k_NodeSize = new Vector2(200, 200);
        static readonly Vector2 k_ReferenceNodePos = new Vector2(SelectionDragger.k_PanAreaWidth, SelectionDragger.k_PanAreaWidth);

        Vector2 m_SnappingNodePos;
        Vector2 m_SelectionOffset = new Vector2(50, 50);

        BasicNodeModel snappingNodeModel { get; set; }
        BasicNodeModel referenceNodeModel { get; set; }

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            GraphViewSettings.UserSettings.EnableSnapToPort = true;
            GraphViewSettings.UserSettings.EnableSnapToBorders = false;
        }

        [UnityTest]
        public IEnumerator HorizontalPortWithinSnappingDistanceShouldSnap()
        {
            // Config (both ports are connected horizontally)
            //   +-------+   +-------+
            //   | Node1 o---o Node2 |
            //   +-------+   +-------+

            m_SnappingNodePos = new Vector2(k_ReferenceNodePos.x + 400, k_ReferenceNodePos.y);

            referenceNodeModel = CreateNode("Node1", k_ReferenceNodePos);
            snappingNodeModel = CreateNode("Node2", m_SnappingNodePos);

            // Add a horizontal port on each node
            var inputPort = snappingNodeModel.AddPort(Orientation.Horizontal, Direction.Input, PortCapacity.Single, typeof(float));
            var outputPort = referenceNodeModel.AddPort(Orientation.Horizontal, Direction.Output, PortCapacity.Single, typeof(float));
            Assert.IsNotNull(outputPort);
            Assert.IsNotNull(inputPort);

            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            // Connect the ports together
            var actions = ConnectPorts(outputPort, inputPort);
            while (actions.MoveNext())
            {
                yield return null;
            }

            // Get the UI nodes
            var snappedNode = snappingNodeModel.GetUI<Node>(graphView);
            var referenceNode = referenceNodeModel.GetUI<Node>(graphView);
            Assert.IsNotNull(snappedNode);
            Assert.IsNotNull(referenceNode);

            Vector2 worldNodePos = graphView.contentViewContainer.LocalToWorld(m_SnappingNodePos);
            Vector2 start = worldNodePos + m_SelectionOffset;

            // We move the snapping node to the max snapping distance
            float offSetY = k_SnappingDistance;
            Vector2 moveOffset = new Vector2(-100, offSetY);

            // Move the snapping node.
            helpers.MouseDownEvent(start);
            yield return null;

            Vector2 end = start + moveOffset;
            helpers.MouseDragEvent(start, end);
            yield return null;

            helpers.MouseUpEvent(end);
            yield return null;

            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            // Get the UI ports
            var outputPortUI = outputPort.GetUI<Port>(graphView);
            var inputPortUI = inputPort.GetUI<Port>(graphView);
            Assert.IsNotNull(outputPortUI);
            Assert.IsNotNull(inputPortUI);

            // The node should snap to the reference node's position in Y, but the X should be dragged normally
            Assert.AreEqual(outputPortUI.GetGlobalCenter().y, inputPortUI.GetGlobalCenter().y);
            Assert.AreNotEqual(m_SnappingNodePos.y + moveOffset.y, snappedNode.layout.y);
            Assert.AreEqual(m_SnappingNodePos.x + moveOffset.x, snappedNode.layout.x);

            yield return null;
        }

        [UnityTest]
        public IEnumerator HorizontalPortNotWithinSnappingDistanceShouldNotSnap()
        {
            // Config (both ports are connected horizontally)
            //   +-------+   +-------+
            //   | Node1 o---o Node2 |
            //   +-------+   +-------+

            m_SnappingNodePos = new Vector2(k_ReferenceNodePos.x + 400, k_ReferenceNodePos.y);

            referenceNodeModel = CreateNode("Node1", k_ReferenceNodePos);
            snappingNodeModel = CreateNode("Node2", m_SnappingNodePos);

            // Add a vertical port on each node
            var inputPort = snappingNodeModel.AddPort(Orientation.Horizontal, Direction.Input, PortCapacity.Single, typeof(float));
            var outputPort = referenceNodeModel.AddPort(Orientation.Horizontal, Direction.Output, PortCapacity.Single, typeof(float));
            Assert.IsNotNull(outputPort);
            Assert.IsNotNull(inputPort);

            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            // Connect the ports together
            var actions = ConnectPorts(outputPort, inputPort);
            while (actions.MoveNext())
            {
                yield return null;
            }

            // Connect the ports together
            var snappedNode = snappingNodeModel.GetUI<Node>(graphView);
            var referenceNode = referenceNodeModel.GetUI<Node>(graphView);
            Assert.IsNotNull(snappedNode);
            Assert.IsNotNull(referenceNode);

            Vector2 worldNodePos = graphView.contentViewContainer.LocalToWorld(m_SnappingNodePos);
            Vector2 start = worldNodePos + m_SelectionOffset;

            // We move the snapping node outside the max snapping distance
            float offSetY = k_SnappingDistance + 1;
            Vector2 moveOffset = new Vector2(-100, offSetY);

            // Move the snapping node
            helpers.MouseDownEvent(start);
            yield return null;

            Vector2 end = start + moveOffset;
            helpers.MouseDragEvent(start, end);
            yield return null;

            helpers.MouseUpEvent(end);
            yield return null;

            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            // Get the UI ports
            var outputPortUI = outputPort.GetUI<Port>(graphView);
            var inputPortUI = inputPort.GetUI<Port>(graphView);
            Assert.IsNotNull(outputPortUI);
            Assert.IsNotNull(inputPortUI);

            // The port should not snap to the reference node's port in Y: the Y and X should be dragged normally
            Assert.AreNotEqual(outputPortUI.GetGlobalCenter().y, inputPortUI.GetGlobalCenter().y);
            Assert.AreEqual(m_SnappingNodePos.x + moveOffset.x, snappedNode.layout.x);
            Assert.AreEqual(m_SnappingNodePos.y + moveOffset.y, snappedNode.layout.y);

            yield return null;
        }

        [UnityTest]
        public IEnumerator VerticalPortWithinSnappingDistanceShouldSnap()
        {
            // Config (both ports are connected vertically)
            //   +-------+
            //   | Node1 o
            //   +-------+
            //   +-------+
            //   o Node2 |
            //   +-------+

            m_SnappingNodePos = new Vector2(k_ReferenceNodePos.x, k_ReferenceNodePos.y + k_NodeSize.y);

            referenceNodeModel = CreateNode("Node1", k_ReferenceNodePos);
            snappingNodeModel = CreateNode("Node2", m_SnappingNodePos);

            // Add a vertical port on each node
            var inputPort = snappingNodeModel.AddPort(Orientation.Vertical, Direction.Input, PortCapacity.Single, typeof(float));
            var outputPort = referenceNodeModel.AddPort(Orientation.Vertical, Direction.Output, PortCapacity.Single, typeof(float));
            Assert.IsNotNull(outputPort);
            Assert.IsNotNull(inputPort);

            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            // Connect the ports together
            var actions = ConnectPorts(outputPort, inputPort);
            while (actions.MoveNext())
            {
                yield return null;
            }

            // Get the UI ports and set their orientation to vertical
            var outputPortUI = outputPort.GetUI<Port>(graphView);
            var inputPortUI = inputPort.GetUI<Port>(graphView);
            Assert.IsNotNull(outputPortUI);
            Assert.IsNotNull(inputPortUI);
            outputPortUI.Orientation = Orientation.Vertical;
            inputPortUI.Orientation = Orientation.Vertical;

            var snappedNode = snappingNodeModel.GetUI<Node>(graphView);
            Assert.IsNotNull(snappedNode);

            Vector2 worldNodePos = graphView.contentViewContainer.LocalToWorld(m_SnappingNodePos);
            Vector2 start = worldNodePos + m_SelectionOffset;

            // We move the snapping node to the max snapping distance to the left
            float outputPortInputPortDistance = Math.Abs(outputPortUI.GetGlobalCenter().x - inputPortUI.GetGlobalCenter().x);
            float offSetX = k_SnappingDistance - outputPortInputPortDistance;

            Vector2 moveOffset = new Vector2(-offSetX, 10);

            // Move the snapping node
            helpers.MouseDownEvent(start);
            yield return null;

            Vector2 end = start + moveOffset;
            helpers.MouseDragEvent(start, end);
            yield return null;

            helpers.MouseUpEvent(end);
            yield return null;

            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            // The node should snap to the reference node's position in X: the Y should be dragged normally
            Assert.AreEqual(outputPortUI.GetGlobalCenter().x, inputPortUI.GetGlobalCenter().x);
            Assert.AreNotEqual(m_SnappingNodePos.x + moveOffset.x, snappedNode.GetPosition().x);
            Assert.AreEqual(m_SnappingNodePos.y + moveOffset.y, snappedNode.GetPosition().y);

            yield return null;
        }

        [UnityTest]
        public IEnumerator VerticalPortNotWithinSnappingDistanceShouldNotSnap()
        {
            // Config (both ports are connected vertically)
            //   +-------+
            //   | Node1 o
            //   +-------+
            //   +-------+
            //   o Node2 |
            //   +-------+
            //
            m_SnappingNodePos = new Vector2(k_ReferenceNodePos.x, k_ReferenceNodePos.y + k_NodeSize.y);

            referenceNodeModel = CreateNode("Node1", k_ReferenceNodePos, 0, 0);
            snappingNodeModel = CreateNode("Node2", m_SnappingNodePos, 0, 0);

            // Add a vertical port on each node
            var inputPort = snappingNodeModel.AddPort(Orientation.Vertical, Direction.Input, PortCapacity.Single, typeof(float));
            var outputPort = referenceNodeModel.AddPort(Orientation.Vertical, Direction.Output, PortCapacity.Single, typeof(float));
            Assert.IsNotNull(outputPort);
            Assert.IsNotNull(inputPort);

            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            // Connect the ports together
            var actions = ConnectPorts(outputPort, inputPort);
            while (actions.MoveNext())
            {
                yield return null;
            }

            // Get the UI ports and set their orientation to vertical
            var outputPortUI = outputPort.GetUI<Port>(graphView);
            var inputPortUI = inputPort.GetUI<Port>(graphView);
            Assert.IsNotNull(outputPortUI);
            Assert.IsNotNull(inputPortUI);
            outputPortUI.Orientation = Orientation.Vertical;
            inputPortUI.Orientation = Orientation.Vertical;

            var snappedNode = snappingNodeModel.GetUI<Node>(graphView);
            Assert.IsNotNull(snappedNode);

            Vector2 worldNodePos = graphView.contentViewContainer.LocalToWorld(m_SnappingNodePos);
            Vector2 start = worldNodePos + m_SelectionOffset;

            // We move the snapping node outside the max snapping distance to the left
            float outputPortInputPortDistance = Math.Abs(outputPortUI.GetGlobalCenter().x - inputPortUI.GetGlobalCenter().x);
            float offSetX = outputPortInputPortDistance - (k_SnappingDistance + 1);

            Vector2 moveOffset = new Vector2(offSetX, 10);

            // Move the snapping node
            helpers.MouseDownEvent(start);
            yield return null;

            Vector2 end = start + moveOffset;
            helpers.MouseDragEvent(start, end);
            yield return null;

            helpers.MouseUpEvent(end);
            yield return null;

            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            // The node should not snap to the reference node's position in X: the X and Y should be dragged normally
            Assert.AreNotEqual(outputPortUI.GetGlobalCenter().x, inputPortUI.GetGlobalCenter().x);
            Assert.AreEqual(m_SnappingNodePos.y + moveOffset.y, snappedNode.GetPosition().y);
            Assert.AreEqual(m_SnappingNodePos.x + moveOffset.x, snappedNode.GetPosition().x);

            yield return null;
        }

        [UnityTest]
        public IEnumerator NodeShouldSnapToNearestConnectedPort()
        {
            // Config (ports are connected horizontally)
            //   +-------+
            //   | Node1 o +-------+
            //   +-------+ o Node2 o +-------+
            //             +-------+ o Node3 |
            //                       +-------+

            referenceNodeModel = CreateNode("Node1", k_ReferenceNodePos);

            m_SnappingNodePos = new Vector2(k_ReferenceNodePos.x + k_NodeSize.x, k_ReferenceNodePos.y + k_NodeSize.y * 0.5f);
            snappingNodeModel = CreateNode("Node2", m_SnappingNodePos);

            // Third node
            Vector2 secondReferenceNodePos = new Vector2(m_SnappingNodePos.x + k_NodeSize.x, m_SnappingNodePos.y + k_NodeSize.y * 0.5f);
            BasicNodeModel secondReferenceNodeModel = CreateNode("Node3", secondReferenceNodePos);

            // Add a horizontal port on each node
            var node1OutputPort = referenceNodeModel.AddPort(Orientation.Horizontal, Direction.Output, PortCapacity.Single, typeof(float));
            var node2InputPort = snappingNodeModel.AddPort(Orientation.Horizontal, Direction.Input, PortCapacity.Single, typeof(float));
            var node2OutputPort = snappingNodeModel.AddPort(Orientation.Horizontal, Direction.Output, PortCapacity.Single, typeof(float));
            var node3InputPort = secondReferenceNodeModel.AddPort(Orientation.Horizontal, Direction.Input, PortCapacity.Single, typeof(float));
            Assert.IsNotNull(node1OutputPort);
            Assert.IsNotNull(node2InputPort);
            Assert.IsNotNull(node2OutputPort);
            Assert.IsNotNull(node3InputPort);

            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            // Connect the ports together
            var actions = ConnectPorts(node1OutputPort, node2InputPort);
            while (actions.MoveNext())
            {
                yield return null;
            }
            actions = ConnectPorts(node2OutputPort, node3InputPort);
            while (actions.MoveNext())
            {
                yield return null;
            }

            Vector2 worldNodePos = graphView.contentViewContainer.LocalToWorld(m_SnappingNodePos);
            Vector2 start = worldNodePos + m_SelectionOffset;

            // We move the snapping Node2 toward reference Node1 within the snapping range
            float offSetY = k_SnappingDistance - k_NodeSize.y * 0.5f;
            Vector2 moveOffset = new Vector2(0, offSetY);

            // Move the snapping node.
            helpers.MouseDownEvent(start);
            yield return null;

            Vector2 end = start + moveOffset;
            helpers.MouseDragEvent(start, end);
            yield return null;

            helpers.MouseUpEvent(end);
            yield return null;

            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            // Get the UI ports
            var node1OutputPortUI = node1OutputPort.GetUI<Port>(graphView);
            var node2InputPortUI = node2InputPort.GetUI<Port>(graphView);
            var node2OutputPortUI = node2OutputPort.GetUI<Port>(graphView);
            var node3InputPortUI = node3InputPort.GetUI<Port>(graphView);
            Assert.IsNotNull(node1OutputPortUI);
            Assert.IsNotNull(node2InputPortUI);
            Assert.IsNotNull(node2OutputPortUI);
            Assert.IsNotNull(node3InputPortUI);

            // The snapping Node2 should snap to Node1's port
            Assert.AreEqual(node1OutputPortUI.GetGlobalCenter().y, node2InputPortUI.GetGlobalCenter().y);
            // The snapping Node2 should not snap to Node3's port
            Assert.AreNotEqual(node3InputPortUI.GetGlobalCenter().y, node2OutputPortUI.GetGlobalCenter().y);

            worldNodePos = graphView.contentViewContainer.LocalToWorld(snappingNodeModel.Position);
            start = worldNodePos + m_SelectionOffset;

            // We move the snapping Node2 toward Node3 within the snapping range
            offSetY = k_NodeSize.y + k_SnappingDistance;
            moveOffset = new Vector2(0, offSetY);

            // Move the snapping node.
            helpers.MouseDownEvent(start);
            yield return null;

            end = start + moveOffset;
            helpers.MouseDragEvent(start, end);
            yield return null;

            helpers.MouseUpEvent(end);
            yield return null;

            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            // The snapping Node2's port should snap to Node3's port
            Assert.AreEqual(node3InputPortUI.GetGlobalCenter().y, node2OutputPortUI.GetGlobalCenter().y);
            // The snapping Node2's port should not snap to Node1's port
            Assert.AreNotEqual(node1OutputPortUI.GetGlobalCenter().y, node2InputPortUI.GetGlobalCenter().y);

            yield return null;
        }

        [UnityTest]
        public IEnumerator NodeUnderMouseShouldSnapWhenMultipleSelectedNodes()
        {
            // Config (ports are connected horizontally)
            //   +-------+
            //   | Node1 o +-------+
            //   +-------+ o Node2 o +-------+
            //             +-------+ o Node3 |
            //                       +-------+

            referenceNodeModel = CreateNode("Node1", k_ReferenceNodePos);

            m_SnappingNodePos = new Vector2(k_ReferenceNodePos.x + k_NodeSize.x, k_ReferenceNodePos.y + k_NodeSize.y * 0.5f);
            snappingNodeModel = CreateNode("Node2", m_SnappingNodePos);

            // Third node
            Vector2 secondSelectedNodePos = new Vector2(m_SnappingNodePos.x + k_NodeSize.x, m_SnappingNodePos.y + k_NodeSize.y * 0.5f);
            BasicNodeModel secondSelectedNodeModel = CreateNode("Node3", secondSelectedNodePos);

            // Add a horizontal port on each node
            var node1OutputPort = referenceNodeModel.AddPort(Orientation.Horizontal, Direction.Output, PortCapacity.Single, typeof(float));
            var node2InputPort = snappingNodeModel.AddPort(Orientation.Horizontal, Direction.Input, PortCapacity.Single, typeof(float));
            var node2OutputPort = snappingNodeModel.AddPort(Orientation.Horizontal, Direction.Output, PortCapacity.Single, typeof(float));
            var node3InputPort = secondSelectedNodeModel.AddPort(Orientation.Horizontal, Direction.Input, PortCapacity.Single, typeof(float));
            Assert.IsNotNull(node1OutputPort);
            Assert.IsNotNull(node2InputPort);
            Assert.IsNotNull(node2OutputPort);
            Assert.IsNotNull(node3InputPort);

            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            // Connect the ports together
            var actions = ConnectPorts(node1OutputPort, node2InputPort);
            while (actions.MoveNext())
            {
                yield return null;
            }
            actions = ConnectPorts(node2OutputPort, node3InputPort);
            while (actions.MoveNext())
            {
                yield return null;
            }

            Vector2 worldPosNode2 = graphView.contentViewContainer.LocalToWorld(m_SnappingNodePos);
            Vector2 worldPosNode3 = graphView.contentViewContainer.LocalToWorld(secondSelectedNodePos);

            Vector2 selectionPosNode2 = worldPosNode2 + m_SelectionOffset;
            Vector2 selectionPosNode3 = worldPosNode3 + m_SelectionOffset;

            // Select Node3 by clicking on it and pressing Ctrl
            helpers.MouseDownEvent(selectionPosNode3, MouseButton.LeftMouse, EventModifiers.Control);
            yield return null;

            helpers.MouseUpEvent(selectionPosNode3, MouseButton.LeftMouse, EventModifiers.Control);
            yield return null;

            // Move mouse to Node2
            helpers.MouseMoveEvent(selectionPosNode3, selectionPosNode2, MouseButton.LeftMouse, EventModifiers.Control);
            yield return null;

            // Select Node2 by clicking on it and pressing Ctrl
            helpers.MouseDownEvent(selectionPosNode2, MouseButton.LeftMouse, EventModifiers.Control);
            yield return null;

            // Move Node2 toward reference Node1 within the snapping range
            float topToTopDistance = k_NodeSize.y * 0.5f;
            float offSetY = k_SnappingDistance - topToTopDistance;
            Vector2 moveOffset = new Vector2(0, offSetY);
            Vector2 end = selectionPosNode2 + moveOffset;
            helpers.MouseDragEvent(selectionPosNode2, end, MouseButton.LeftMouse, EventModifiers.Control);
            yield return null;

            helpers.MouseUpEvent(end, MouseButton.LeftMouse, EventModifiers.Control);
            yield return null;

            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            // Get the UI ports
            var node1OutputPortUI = node1OutputPort.GetUI<Port>(graphView);
            var node2InputPortUI = node2InputPort.GetUI<Port>(graphView);
            var node2OutputPortUI = node2OutputPort.GetUI<Port>(graphView);
            var node3InputPortUI = node3InputPort.GetUI<Port>(graphView);
            var node3 = secondSelectedNodeModel.GetUI<Node>(graphView);
            Assert.IsNotNull(node1OutputPortUI);
            Assert.IsNotNull(node2InputPortUI);
            Assert.IsNotNull(node2OutputPortUI);
            Assert.IsNotNull(node3InputPortUI);
            Assert.IsNotNull(node3);

            // The snapping Node2 should snap to Node1's port
            Assert.AreEqual(node1OutputPortUI.GetGlobalCenter().y, node2InputPortUI.GetGlobalCenter().y);
            // Node3 should not snap to Node1's port
            Assert.AreNotEqual(node1OutputPortUI.GetGlobalCenter().y, node3InputPortUI.GetGlobalCenter().y);
            // Node 3 should have moved by the move offset in x and the same y offset as Node2
            Assert.AreEqual(secondSelectedNodePos.x + moveOffset.x, node3.layout.x);
            Assert.AreEqual(secondSelectedNodePos.y - topToTopDistance, node3.layout.y);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ShouldSnapToClosestPortWhenMultipleConnectedPorts()
        {
            // Config (both node1 ports are connected horizontally to node2's port)
            //   +-------+   +-------+
            //   | Node1 o   o Node2 |
            //   |       o   +-------+
            //   +-------+

            m_SnappingNodePos = new Vector2(k_ReferenceNodePos.x + k_NodeSize.x, k_ReferenceNodePos.y);

            referenceNodeModel = CreateNode("Node1", k_ReferenceNodePos);
            snappingNodeModel = CreateNode("Node2", m_SnappingNodePos);

            // Add horizontal ports on each node
            var node1FirstOutputPort = referenceNodeModel.AddPort(Orientation.Horizontal, Direction.Output, PortCapacity.Single, typeof(float));
            var node1SecondOutputPort = referenceNodeModel.AddPort(Orientation.Horizontal, Direction.Output, PortCapacity.Single, typeof(float));
            var node2InputPort = snappingNodeModel.AddPort(Orientation.Horizontal, Direction.Input, PortCapacity.Multi, typeof(float));
            Assert.IsNotNull(node1FirstOutputPort);
            Assert.IsNotNull(node1SecondOutputPort);
            Assert.IsNotNull(node2InputPort);

            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            // Connect the ports together
            var actions = ConnectPorts(node1FirstOutputPort, node2InputPort);
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = ConnectPorts(node1SecondOutputPort, node2InputPort);
            while (actions.MoveNext())
            {
                yield return null;
            }

            Vector2 worldNodePos = graphView.contentViewContainer.LocalToWorld(m_SnappingNodePos);
            Vector2 start = worldNodePos + m_SelectionOffset;

            // We move the snapping node closer to the first output port within the snapping range
            float offSetY = -k_SnappingDistance;
            Vector2 moveOffset = new Vector2(0, offSetY);

            // Move the snapping node.
            helpers.MouseDownEvent(start);
            yield return null;

            Vector2 end = start + moveOffset;
            helpers.MouseDragEvent(start, end);
            yield return null;

            helpers.MouseUpEvent(end);
            yield return null;

            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            // Get the UI ports
            var node1FirstOutputPortUI = node1FirstOutputPort.GetUI<Port>(graphView);
            var node1SecondOutputPortUI = node1SecondOutputPort.GetUI<Port>(graphView);
            var node2InputPortUI = node2InputPort.GetUI<Port>(graphView);
            Assert.IsNotNull(node1FirstOutputPortUI);
            Assert.IsNotNull(node1SecondOutputPortUI);
            Assert.IsNotNull(node2InputPortUI);

            // The snapping Node2's port should snap to Node1's first output port
            Assert.AreEqual(node1FirstOutputPortUI.GetGlobalCenter().y, node2InputPortUI.GetGlobalCenter().y);
            Assert.AreNotEqual(node1SecondOutputPortUI.GetGlobalCenter().y, node2InputPortUI.GetGlobalCenter().y);

            // We move the snapping node closer to the second output port within the snapping range
            offSetY = node1SecondOutputPortUI.GetGlobalCenter().y - node2InputPortUI.GetGlobalCenter().y - k_SnappingDistance;
            moveOffset = new Vector2(0, offSetY);

            // Move the snapping node.
            helpers.MouseDownEvent(start);
            yield return null;

            end = start + moveOffset;
            helpers.MouseDragEvent(start, end);
            yield return null;

            helpers.MouseUpEvent(end);
            yield return null;

            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            // The snapping Node2's port should snap to Node1's second output port
            Assert.AreEqual(node1SecondOutputPortUI.GetGlobalCenter().y, node2InputPortUI.GetGlobalCenter().y);
            Assert.AreNotEqual(node1FirstOutputPortUI.GetGlobalCenter().y, node2InputPortUI.GetGlobalCenter().y);

            yield return null;
        }
    }

    public class SnapToBordersTests : GraphViewTester
    {
        const float k_SnapDistance = 8.0f;
        static readonly Vector2 k_ReferenceNodePos = new Vector2(SelectionDragger.k_PanAreaWidth, SelectionDragger.k_PanAreaWidth);

        Vector2 m_SnappingNodePos;
        Vector2 m_SelectionOffset = new Vector2(50, 50);

        BasicNodeModel snappingNodeModel { get; set; }
        BasicNodeModel referenceNodeModel { get; set; }

        public void UpdateUINodeSize(ref Node node, BasicNodeModel nodeModel, float height, float width)
        {
            // Get the UI nodes
            node = nodeModel.GetUI<Node>(graphView);
            Assert.IsNotNull(node);

            // Changing the reference node's height to make it easier to test the snapping
            node.style.height = height;
            node.style.width = width;
        }

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            GraphViewSettings.UserSettings.EnableSnapToPort = false;
            GraphViewSettings.UserSettings.EnableSnapToBorders = true;
        }

        [UnityTest]
        public IEnumerator ElementTopBorderShouldSnapToTopBorder()
        {
            // Config
            //   +-------+
            //   | Node1 |    +-------+
            //   |       |    | Node2 |
            //   |       |    +-------+
            //   +-------+

            m_SnappingNodePos = new Vector2(k_ReferenceNodePos.x + 100, k_ReferenceNodePos.y + 250);

            referenceNodeModel = CreateNode("Node1", k_ReferenceNodePos + new Vector2(0, 200));
            snappingNodeModel = CreateNode("Node2", m_SnappingNodePos);

            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            // Get the UI nodes
            var snappedNode = snappingNodeModel.GetUI<Node>(graphView);
            var referenceNode = referenceNodeModel.GetUI<Node>(graphView);
            Assert.IsNotNull(snappedNode);
            Assert.IsNotNull(referenceNode);

            // Changing the nodes' sizes to make it easier to test the snapping
            UpdateUINodeSize(ref snappedNode, snappingNodeModel, 100, 100);
            UpdateUINodeSize(ref referenceNode, referenceNodeModel, 200, 100);
            yield return null;

            Vector2 worldNodePos = graphView.contentViewContainer.LocalToWorld(m_SnappingNodePos);
            Vector2 start = worldNodePos + m_SelectionOffset;

            // TOP BORDER TO TOP BORDER: We move the snapping node so that its top border snaps with the reference node's top border
            float topToTopDistance = snappedNode.layout.yMin - referenceNode.layout.yMin;
            float offSetY = k_SnapDistance - topToTopDistance;
            Vector2 moveOffset = new Vector2(10, offSetY);

            // Move the snapping node.
            helpers.MouseDownEvent(start);
            yield return null;

            Vector2 end = start + moveOffset;
            helpers.MouseDragEvent(start, end);
            yield return null;

            helpers.MouseUpEvent(end);
            yield return null;

            // The snapping node's top border should snap to the reference node's top border, but the X should be dragged normally
            Assert.AreEqual(referenceNode.layout.yMin, snappedNode.layout.yMin);
            Assert.AreNotEqual(m_SnappingNodePos.y + moveOffset.y, snappedNode.layout.y);
            Assert.AreEqual(m_SnappingNodePos.x + moveOffset.x, snappedNode.layout.x);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementTopBorderShouldSnapToBottomBorder()
        {
            // Config
            //   +-------+
            //   | Node1 |    +-------+
            //   |       |    | Node2 |
            //   |       |    +-------+
            //   +-------+

            m_SnappingNodePos = new Vector2(k_ReferenceNodePos.x + 100, k_ReferenceNodePos.y + 50);

            referenceNodeModel = CreateNode("Node1", k_ReferenceNodePos);
            snappingNodeModel = CreateNode("Node2", m_SnappingNodePos);

            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            // Get the UI nodes
            var snappedNode = snappingNodeModel.GetUI<Node>(graphView);
            var referenceNode = referenceNodeModel.GetUI<Node>(graphView);
            Assert.IsNotNull(snappedNode);
            Assert.IsNotNull(referenceNode);

            // Changing the nodes' sizes to make it easier to test the snapping
            UpdateUINodeSize(ref snappedNode, snappingNodeModel, 100, 100);
            UpdateUINodeSize(ref referenceNode, referenceNodeModel, 200, 100);
            yield return null;

            Vector2 worldNodePos = graphView.contentViewContainer.LocalToWorld(m_SnappingNodePos);
            Vector2 start = worldNodePos + m_SelectionOffset;

            // TOP BORDER TO BOTTOM BORDER: We move the snapping node so that its top border snaps with the reference node's bottom border
            float topToBottomDistance = referenceNode.layout.yMax - snappedNode.layout.yMin;
            float offSetY =  topToBottomDistance - k_SnapDistance;
            Vector2 moveOffset = new Vector2(10, offSetY);

            // Move the snapping node.
            helpers.MouseDownEvent(start);
            yield return null;

            var end = start + moveOffset;
            helpers.MouseDragEvent(start, end);
            yield return null;

            helpers.MouseUpEvent(end);
            yield return null;

            // The snapping node's top border should snap to the reference node's bottom border in Y, but the X should be dragged normally
            Assert.AreEqual(referenceNode.layout.yMax, snappedNode.layout.yMin);
            Assert.AreNotEqual(m_SnappingNodePos.y + moveOffset.y, snappedNode.layout.y);
            Assert.AreEqual(m_SnappingNodePos.x + moveOffset.x, snappedNode.layout.x);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementCenterYBorderShouldSnapToTopBorder()
        {
            // Config
            //   +-------+
            //   | Node1 |    +-------+
            //   |       |    | Node2 |
            //   |       |    +-------+
            //   +-------+

            m_SnappingNodePos = new Vector2(k_ReferenceNodePos.x + 100, k_ReferenceNodePos.y + 250);

            referenceNodeModel = CreateNode("Node1", k_ReferenceNodePos + new Vector2(0, 200));
            snappingNodeModel = CreateNode("Node2", m_SnappingNodePos);

            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            // Get the UI nodes
            var snappedNode = snappingNodeModel.GetUI<Node>(graphView);
            var referenceNode = referenceNodeModel.GetUI<Node>(graphView);
            Assert.IsNotNull(snappedNode);
            Assert.IsNotNull(referenceNode);

            // Changing the nodes' sizes to make it easier to test the snapping
            UpdateUINodeSize(ref snappedNode, snappingNodeModel, 100, 100);
            UpdateUINodeSize(ref referenceNode, referenceNodeModel, 200, 100);
            yield return null;

            Vector2 worldNodePos = graphView.contentViewContainer.LocalToWorld(m_SnappingNodePos);
            Vector2 start = worldNodePos + m_SelectionOffset;

            // CENTER TO TOP BORDER: We move the snapping node so that its center border snaps with the reference node's top border
            float centerToTopDistance = snappedNode.layout.center.y - referenceNode.layout.yMin;
            float offSetY = k_SnapDistance - centerToTopDistance;
            Vector2 moveOffset = new Vector2(10, offSetY);

            // Move the snapping node.
            helpers.MouseDownEvent(start);
            yield return null;

            Vector2 end = start + moveOffset;
            helpers.MouseDragEvent(start, end);
            yield return null;

            helpers.MouseUpEvent(end);
            yield return null;

            // The snapping node's center should snap to the reference node's top border in Y, but the X should be dragged normally
            Assert.AreEqual(referenceNode.layout.yMin, snappedNode.layout.center.y);
            Assert.AreNotEqual(m_SnappingNodePos.y + moveOffset.y, snappedNode.layout.y);
            Assert.AreEqual(m_SnappingNodePos.x + moveOffset.x, snappedNode.layout.x);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementCenterYBorderShouldSnapToBottomBorder()
        {
            // Config
            //   +-------+
            //   | Node1 |    +-------+
            //   |       |    | Node2 |
            //   |       |    +-------+
            //   +-------+

            m_SnappingNodePos = new Vector2(k_ReferenceNodePos.x + 100, k_ReferenceNodePos.y + 50);

            referenceNodeModel = CreateNode("Node1", k_ReferenceNodePos);
            snappingNodeModel = CreateNode("Node2", m_SnappingNodePos);

            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            // Get the UI nodes
            var snappedNode = snappingNodeModel.GetUI<Node>(graphView);
            var referenceNode = referenceNodeModel.GetUI<Node>(graphView);
            Assert.IsNotNull(snappedNode);
            Assert.IsNotNull(referenceNode);

            // Changing the nodes' sizes to make it easier to test the snapping
            UpdateUINodeSize(ref snappedNode, snappingNodeModel, 100, 100);
            UpdateUINodeSize(ref referenceNode, referenceNodeModel, 200, 100);
            yield return null;

            Vector2 worldNodePos = graphView.contentViewContainer.LocalToWorld(m_SnappingNodePos);
            Vector2 start = worldNodePos + m_SelectionOffset;

            // CENTER TO BOTTOM BORDER: We move the snapping node so that its center border snaps with the reference node's bottom border
            float centerToBottomDistance = referenceNode.layout.yMax - snappedNode.layout.center.y;
            float offSetY = centerToBottomDistance - k_SnapDistance;
            Vector2 moveOffset = new Vector2(10, offSetY);

            // Move the snapping node.
            helpers.MouseDownEvent(start);
            yield return null;

            Vector2 end = start + moveOffset;
            helpers.MouseDragEvent(start, end);
            yield return null;

            helpers.MouseUpEvent(end);
            yield return null;

            // The snapping node's center should snap to the reference node's bottom border in Y, but the X should be dragged normally
            Assert.AreEqual(referenceNode.layout.yMax, snappedNode.layout.center.y);
            Assert.AreNotEqual(m_SnappingNodePos.y + moveOffset.y, snappedNode.layout.y);
            Assert.AreEqual(m_SnappingNodePos.x + moveOffset.x, snappedNode.layout.x);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementBottomBorderShouldSnapToTopBorder()
        {
            // Config
            //   +-------+
            //   | Node1 |    +-------+
            //   |       |    | Node2 |
            //   |       |    +-------+
            //   +-------+

            m_SnappingNodePos = new Vector2(k_ReferenceNodePos.x + 100, k_ReferenceNodePos.y + 250);

            referenceNodeModel = CreateNode("Node1", k_ReferenceNodePos + new Vector2(0, 200));
            snappingNodeModel = CreateNode("Node2", m_SnappingNodePos);

            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            // Get the UI nodes
            var snappedNode = snappingNodeModel.GetUI<Node>(graphView);
            var referenceNode = referenceNodeModel.GetUI<Node>(graphView);
            Assert.IsNotNull(snappedNode);
            Assert.IsNotNull(referenceNode);

            // Changing the nodes' sizes to make it easier to test the snapping
            UpdateUINodeSize(ref snappedNode, snappingNodeModel, 100, 100);
            UpdateUINodeSize(ref referenceNode, referenceNodeModel, 200, 100);
            yield return null;

            Vector2 worldNodePos = graphView.contentViewContainer.LocalToWorld(m_SnappingNodePos);
            Vector2 start = worldNodePos + m_SelectionOffset;

            // BOTTOM TO TOP BORDER: We move the snapping node so that its bottom border snaps with the reference node's top border
            float bottomToTopDistance = snappedNode.layout.yMax - referenceNode.layout.yMin;
            float offSetY =  k_SnapDistance - bottomToTopDistance;
            Vector2 moveOffset = new Vector2(10, offSetY);

            // Move the snapping node.
            helpers.MouseDownEvent(start);
            yield return null;

            Vector2 end = start + moveOffset;
            helpers.MouseDragEvent(start, end);
            yield return null;

            helpers.MouseUpEvent(end);
            yield return null;

            // The snapping node's bottom should snap to the reference node's top border in Y, but the X should be dragged normally
            Assert.AreEqual(referenceNode.layout.yMin, snappedNode.layout.yMax);
            Assert.AreNotEqual(m_SnappingNodePos.y + moveOffset.y, snappedNode.layout.y);
            Assert.AreEqual(m_SnappingNodePos.x + moveOffset.x, snappedNode.layout.x);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementBottomBorderShouldSnapToBottomBorder()
        {
            // Config
            //   +-------+
            //   | Node1 |    +-------+
            //   |       |    | Node2 |
            //   |       |    +-------+
            //   +-------+

            m_SnappingNodePos = new Vector2(k_ReferenceNodePos.x + 100, k_ReferenceNodePos.y + 50);

            referenceNodeModel = CreateNode("Node1", k_ReferenceNodePos);
            snappingNodeModel = CreateNode("Node2", m_SnappingNodePos);

            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            // Get the UI nodes
            var snappedNode = snappingNodeModel.GetUI<Node>(graphView);
            var referenceNode = referenceNodeModel.GetUI<Node>(graphView);
            Assert.IsNotNull(snappedNode);
            Assert.IsNotNull(referenceNode);

            // Changing the nodes' sizes to make it easier to test the snapping
            UpdateUINodeSize(ref snappedNode, snappingNodeModel, 100, 100);
            UpdateUINodeSize(ref referenceNode, referenceNodeModel, 200, 100);
            yield return null;

            Vector2 worldNodePos = graphView.contentViewContainer.LocalToWorld(m_SnappingNodePos);
            Vector2 start = worldNodePos + m_SelectionOffset;

            // BOTTOM TO BOTTOM BORDER: We move the snapping node so that its bottom border snaps with the reference node's bottom border
            float bottomToBottomDistance = referenceNode.layout.yMax - snappedNode.layout.yMax;
            float offSetY =  k_SnapDistance + bottomToBottomDistance;
            Vector2 moveOffset = new Vector2(10, offSetY);

            // Move the snapping node.
            helpers.MouseDownEvent(start);
            yield return null;

            Vector2 end = start + moveOffset;
            helpers.MouseDragEvent(start, end);
            yield return null;

            helpers.MouseUpEvent(end);
            yield return null;

            // The snapping node's center should snap to the reference node's top border in Y, but the X should be dragged normally
            Assert.AreEqual(referenceNode.layout.yMax, snappedNode.layout.yMax);
            Assert.AreNotEqual(m_SnappingNodePos.y + moveOffset.y, snappedNode.layout.y);
            Assert.AreEqual(m_SnappingNodePos.x + moveOffset.x, snappedNode.layout.x);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementTopBorderShouldNotSnapToTopBorder()
        {
            // Config
            //   +-------+
            //   | Node1 |    +-------+
            //   |       |    | Node2 |
            //   |       |    +-------+
            //   +-------+

            m_SnappingNodePos = new Vector2(k_ReferenceNodePos.x + 100, k_ReferenceNodePos.y + 250);

            referenceNodeModel = CreateNode("Node1", k_ReferenceNodePos + new Vector2(0, 200));
            snappingNodeModel = CreateNode("Node2", m_SnappingNodePos);

            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            // Get the UI nodes
            var snappedNode = snappingNodeModel.GetUI<Node>(graphView);
            var referenceNode = referenceNodeModel.GetUI<Node>(graphView);
            Assert.IsNotNull(snappedNode);
            Assert.IsNotNull(referenceNode);

            // Changing the nodes' sizes to make it easier to test the snapping
            UpdateUINodeSize(ref snappedNode, snappingNodeModel, 100, 100);
            UpdateUINodeSize(ref referenceNode, referenceNodeModel, 200, 100);
            yield return null;

            /*** TOP BORDER SNAPPING ***/
            Vector2 worldNodePos = graphView.contentViewContainer.LocalToWorld(m_SnappingNodePos);
            Vector2 start = worldNodePos + m_SelectionOffset;

            // TOP BORDER TO TOP BORDER: We move the snapping node outside the snapping distance
            float topToTopDistance = snappedNode.layout.yMin - referenceNode.layout.yMin;
            float offSetY = (k_SnapDistance + 1) - topToTopDistance;
            Vector2 moveOffset = new Vector2(10, offSetY);

            // Move the snapping node.
            helpers.MouseDownEvent(start);
            yield return null;

            Vector2 end = start + moveOffset;
            helpers.MouseDragEvent(start, end);
            yield return null;

            helpers.MouseUpEvent(end);
            yield return null;

            // The snapping node's top border should not snap to the reference node's top border, but the X and Y should be dragged normally
            Assert.AreNotEqual(referenceNode.layout.yMin, snappedNode.layout.yMin);
            Assert.AreEqual(m_SnappingNodePos.y + moveOffset.y, snappedNode.layout.y);
            Assert.AreEqual(m_SnappingNodePos.x + moveOffset.x, snappedNode.layout.x);
        }

        [UnityTest]
        public IEnumerator ElementTopBorderShouldNotSnapToBottomBorder()
        {
            // Config
            //   +-------+
            //   | Node1 |    +-------+
            //   |       |    | Node2 |
            //   |       |    +-------+
            //   +-------+

            m_SnappingNodePos = new Vector2(k_ReferenceNodePos.x + 100, k_ReferenceNodePos.y + 50);

            referenceNodeModel = CreateNode("Node1", k_ReferenceNodePos);
            snappingNodeModel = CreateNode("Node2", m_SnappingNodePos);

            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            // Get the UI nodes
            var snappedNode = snappingNodeModel.GetUI<Node>(graphView);
            var referenceNode = referenceNodeModel.GetUI<Node>(graphView);
            Assert.IsNotNull(snappedNode);
            Assert.IsNotNull(referenceNode);

            // Changing the nodes' sizes to make it easier to test the snapping
            UpdateUINodeSize(ref snappedNode, snappingNodeModel, 100, 100);
            UpdateUINodeSize(ref referenceNode, referenceNodeModel, 200, 100);
            yield return null;;

            Vector2 worldNodePos = graphView.contentViewContainer.LocalToWorld(m_SnappingNodePos);
            Vector2 start = worldNodePos + m_SelectionOffset;

            // TOP BORDER TO BOTTOM BORDER: We move the snapping node outside the snapping distance
            float topToTopDistance = snappedNode.layout.yMin - referenceNode.layout.yMin;
            float offSetY = (k_SnapDistance + 1) - topToTopDistance;
            Vector2 moveOffset = new Vector2(10, offSetY);

            // Move the snapping node.
            helpers.MouseDownEvent(start);
            yield return null;

            Vector2 end = start + moveOffset;
            helpers.MouseDragEvent(start, end);
            yield return null;

            helpers.MouseUpEvent(end);
            yield return null;

            // The snapping node's top border should not snap to the reference node's top border: the X and Y should be dragged normally
            Assert.AreNotEqual(referenceNode.layout.yMin, snappedNode.layout.yMin);
            Assert.AreEqual(m_SnappingNodePos.y + moveOffset.y, snappedNode.layout.y);
            Assert.AreEqual(m_SnappingNodePos.x + moveOffset.x, snappedNode.layout.x);
        }

        [UnityTest]
        public IEnumerator ElementCenterYBorderShouldNotSnapToTopBorder()
        {
            // Config
            //   +-------+
            //   | Node1 |    +-------+
            //   |       |    | Node2 |
            //   |       |    +-------+
            //   +-------+

            m_SnappingNodePos = new Vector2(k_ReferenceNodePos.x + 100, k_ReferenceNodePos.y + 250);

            referenceNodeModel = CreateNode("Node1", k_ReferenceNodePos + new Vector2(0, 200));
            snappingNodeModel = CreateNode("Node2", m_SnappingNodePos);

            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            // Get the UI nodes
            var snappedNode = snappingNodeModel.GetUI<Node>(graphView);
            var referenceNode = referenceNodeModel.GetUI<Node>(graphView);
            Assert.IsNotNull(snappedNode);
            Assert.IsNotNull(referenceNode);

            // Changing the nodes' sizes to make it easier to test the snapping
            UpdateUINodeSize(ref snappedNode, snappingNodeModel, 100, 100);
            UpdateUINodeSize(ref referenceNode, referenceNodeModel, 200, 100);
            yield return null;

            Vector2 worldNodePos = graphView.contentViewContainer.LocalToWorld(m_SnappingNodePos);
            Vector2 start = worldNodePos + m_SelectionOffset;

            // CENTER TO TOP BORDER: We move the snapping node outside the snapping distance
            float centerToTopDistance = snappedNode.layout.center.y - referenceNode.layout.yMin;
            float offSetY = (k_SnapDistance + 1) - centerToTopDistance;
            Vector2 moveOffset = new Vector2(10, offSetY);

            // Move the snapping node.
            helpers.MouseDownEvent(start);
            yield return null;

            Vector2 end = start + moveOffset;
            helpers.MouseDragEvent(start, end);
            yield return null;

            helpers.MouseUpEvent(end);
            yield return null;

            // The snapping node's center should not snap to the reference node's top border in Y: the X and Y should be dragged normally
            Assert.AreNotEqual(referenceNode.layout.yMin, snappedNode.layout.center.y);
            Assert.AreEqual(m_SnappingNodePos.y + moveOffset.y, snappedNode.layout.y);
            Assert.AreEqual(m_SnappingNodePos.x + moveOffset.x, snappedNode.layout.x);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementCenterYBorderShouldNotSnapToBottomBorder()
        {
            // Config
            //   +-------+
            //   | Node1 |    +-------+
            //   |       |    | Node2 |
            //   |       |    +-------+
            //   +-------+

            m_SnappingNodePos = new Vector2(k_ReferenceNodePos.x + 100, k_ReferenceNodePos.y + 50);

            referenceNodeModel = CreateNode("Node1", k_ReferenceNodePos);
            snappingNodeModel = CreateNode("Node2", m_SnappingNodePos);

            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            // Get the UI nodes
            var snappedNode = snappingNodeModel.GetUI<Node>(graphView);
            var referenceNode = referenceNodeModel.GetUI<Node>(graphView);
            Assert.IsNotNull(snappedNode);
            Assert.IsNotNull(referenceNode);

            // Changing the nodes' sizes to make it easier to test the snapping
            UpdateUINodeSize(ref snappedNode, snappingNodeModel, 100, 100);
            UpdateUINodeSize(ref referenceNode, referenceNodeModel, 200, 100);
            yield return null;

            Vector2 worldNodePos = graphView.contentViewContainer.LocalToWorld(m_SnappingNodePos);
            Vector2 start = worldNodePos + m_SelectionOffset;

            // CENTER TO BOTTOM BORDER: We move the snapping node outside the snapping distance
            float centerToBottomDistance = referenceNode.layout.yMax - snappedNode.layout.center.y;
            float offSetY = centerToBottomDistance - (k_SnapDistance + 1);
            Vector2 moveOffset = new Vector2(10, offSetY);

            // Move the snapping node.
            helpers.MouseDownEvent(start);
            yield return null;

            Vector2 end = start + moveOffset;
            helpers.MouseDragEvent(start, end);
            yield return null;

            helpers.MouseUpEvent(end);
            yield return null;

            // The snapping node's center should not snap to the reference node's bottom border in Y: the X and Y should be dragged normally
            Assert.AreNotEqual(referenceNode.layout.yMax, snappedNode.layout.center.y);
            Assert.AreEqual(m_SnappingNodePos.y + moveOffset.y, snappedNode.layout.y);
            Assert.AreEqual(m_SnappingNodePos.x + moveOffset.x, snappedNode.layout.x);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementBottomBorderShouldNotSnapToTopBorder()
        {
            // Config
            //   +-------+
            //   | Node1 |    +-------+
            //   |       |    | Node2 |
            //   |       |    +-------+
            //   +-------+

            m_SnappingNodePos = new Vector2(k_ReferenceNodePos.x + 100, k_ReferenceNodePos.y + 250);

            referenceNodeModel = CreateNode("Node1", k_ReferenceNodePos + new Vector2(0, 200));
            snappingNodeModel = CreateNode("Node2", m_SnappingNodePos);

            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            // Get the UI nodes
            var snappedNode = snappingNodeModel.GetUI<Node>(graphView);
            var referenceNode = referenceNodeModel.GetUI<Node>(graphView);
            Assert.IsNotNull(snappedNode);
            Assert.IsNotNull(referenceNode);

            // Changing the nodes' sizes to make it easier to test the snapping
            UpdateUINodeSize(ref snappedNode, snappingNodeModel, 100, 100);
            UpdateUINodeSize(ref referenceNode, referenceNodeModel, 200, 100);
            yield return null;

            Vector2 worldNodePos = graphView.contentViewContainer.LocalToWorld(m_SnappingNodePos);
            Vector2 start = worldNodePos + m_SelectionOffset;

            // BOTTOM TO TOP BORDER: We move the snapping node outside the max snapping distance
            float bottomToTopDistance = snappedNode.layout.yMax - referenceNode.layout.yMin;
            float offSetY =  (k_SnapDistance + 1) - bottomToTopDistance;
            Vector2 moveOffset = new Vector2(10, offSetY);

            // Move the snapping node.
            helpers.MouseDownEvent(start);
            yield return null;

            Vector2 end = start + moveOffset;
            helpers.MouseDragEvent(start, end);
            yield return null;

            helpers.MouseUpEvent(end);
            yield return null;

            // The snapping node's bottom should not snap to the reference node's top border in Y: the X and Y should be dragged normally
            Assert.AreNotEqual(referenceNode.layout.yMin, snappedNode.layout.yMax);
            Assert.AreEqual(m_SnappingNodePos.y + moveOffset.y, snappedNode.layout.y);
            Assert.AreEqual(m_SnappingNodePos.x + moveOffset.x, snappedNode.layout.x);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementBottomBorderShouldNotSnapToBottomBorder()
        {
            // Config
            //   +-------+
            //   | Node1 |    +-------+
            //   |       |    | Node2 |
            //   |       |    +-------+
            //   +-------+

            m_SnappingNodePos = new Vector2(k_ReferenceNodePos.x + 100, k_ReferenceNodePos.y + 50);

            referenceNodeModel = CreateNode("Node1", k_ReferenceNodePos);
            snappingNodeModel = CreateNode("Node2", m_SnappingNodePos);

            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            // Get the UI nodes
            var snappedNode = snappingNodeModel.GetUI<Node>(graphView);
            var referenceNode = referenceNodeModel.GetUI<Node>(graphView);
            Assert.IsNotNull(snappedNode);
            Assert.IsNotNull(referenceNode);

            // Changing the nodes' sizes to make it easier to test the snapping
            UpdateUINodeSize(ref snappedNode, snappingNodeModel, 100, 100);
            UpdateUINodeSize(ref referenceNode, referenceNodeModel, 200, 100);
            yield return null;

            Vector2 worldNodePos = graphView.contentViewContainer.LocalToWorld(m_SnappingNodePos);
            Vector2 start = worldNodePos + m_SelectionOffset;

            // BOTTOM TO BOTTOM BORDER: We move the snapping node outside the max snapping distance
            float bottomToBottomDistance = referenceNode.layout.yMax - snappedNode.layout.yMax;
            float offSetY = (k_SnapDistance + 1) + bottomToBottomDistance;
            Vector2 moveOffset = new Vector2(10, offSetY);

            // Move the snapping node.
            helpers.MouseDownEvent(start);
            yield return null;

            Vector2 end = start + moveOffset;
            helpers.MouseDragEvent(start, end);
            yield return null;

            helpers.MouseUpEvent(end);
            yield return null;

            // The snapping node's center should not snap to the reference node's top border in Y: the X and Y should be dragged normally
            Assert.AreNotEqual(referenceNode.layout.yMax, snappedNode.layout.yMax);
            Assert.AreEqual(m_SnappingNodePos.y + moveOffset.y, snappedNode.layout.y);
            Assert.AreEqual(m_SnappingNodePos.x + moveOffset.x, snappedNode.layout.x);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementLeftBorderShouldSnapToLeftBorder()
        {
            // Config
            //
            //  +-------------+
            //  |    Node1    |
            //  +-------------+
            //     +-------+
            //     | Node2 |
            //     +-------+

            referenceNodeModel = CreateNode("Node1", k_ReferenceNodePos + new Vector2(100, 0));

            m_SnappingNodePos = new Vector2(k_ReferenceNodePos.x + 50, k_ReferenceNodePos.y + 100);
            snappingNodeModel = CreateNode("Node2", m_SnappingNodePos);

            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            // Get the UI nodes
            var snappedNode = snappingNodeModel.GetUI<Node>(graphView);
            var referenceNode = referenceNodeModel.GetUI<Node>(graphView);
            Assert.IsNotNull(snappedNode);
            Assert.IsNotNull(referenceNode);

            // Changing the nodes' sizes to make it easier to test the snapping
            UpdateUINodeSize(ref snappedNode, snappingNodeModel, 100, 100);
            UpdateUINodeSize(ref referenceNode, referenceNodeModel, 100, 200);
            yield return null;

            Vector2 worldNodePos = graphView.contentViewContainer.LocalToWorld(m_SnappingNodePos);
            Vector2 start = worldNodePos + m_SelectionOffset;

            // LEFT TO LEFT BORDER:
            float leftToLeftDistance = snappedNode.layout.xMin - referenceNode.layout.xMin;
            float offSetX = k_SnapDistance - leftToLeftDistance;
            Vector2 moveOffset = new Vector2(offSetX, 10);

            // Move the snapping node.
            helpers.MouseDownEvent(start);
            yield return null;

            Vector2 end = start + moveOffset;
            helpers.MouseDragEvent(start, end);
            yield return null;

            helpers.MouseUpEvent(end);
            yield return null;

            // The snapping node's left border should snap to the reference node's left border in X, but Y should be dragged normally
            Assert.AreEqual(referenceNode.layout.xMin, snappedNode.layout.xMin);
            Assert.AreNotEqual(m_SnappingNodePos.x + moveOffset.x, snappedNode.layout.x);
            Assert.AreEqual(m_SnappingNodePos.y + moveOffset.y, snappedNode.layout.y);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementLeftBorderShouldSnapToRightBorder()
        {
            // Config
            //
            //  +-------------+
            //  |    Node1    |
            //  +-------------+
            //     +-------+
            //     | Node2 |
            //     +-------+

            m_SnappingNodePos = new Vector2(k_ReferenceNodePos.x + 50, k_ReferenceNodePos.y + 100);

            referenceNodeModel = CreateNode("Node1", k_ReferenceNodePos);
            snappingNodeModel = CreateNode("Node2", m_SnappingNodePos);

            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            // Get the UI nodes
            var snappedNode = snappingNodeModel.GetUI<Node>(graphView);
            var referenceNode = referenceNodeModel.GetUI<Node>(graphView);
            Assert.IsNotNull(snappedNode);
            Assert.IsNotNull(referenceNode);

            // Changing the nodes' sizes to make it easier to test the snapping
            UpdateUINodeSize(ref snappedNode, snappingNodeModel, 100, 100);
            UpdateUINodeSize(ref referenceNode, referenceNodeModel, 100, 200);
            yield return null;

            Vector2 worldNodePos = graphView.contentViewContainer.LocalToWorld(m_SnappingNodePos);
            Vector2 start = worldNodePos + m_SelectionOffset;

            // LEFT TO RIGHT BORDER:
            float leftToRightDistance = Math.Abs(referenceNode.layout.xMax - snappedNode.layout.xMin);
            float offSetX = k_SnapDistance + leftToRightDistance;
            Vector2 moveOffset = new Vector2(offSetX, 10);

            // Move the snapping node.
            helpers.MouseDownEvent(start);
            yield return null;

            Vector2 end = start + moveOffset;
            helpers.MouseDragEvent(start, end);
            yield return null;

            helpers.MouseUpEvent(end);
            yield return null;

            // The snapping node's left border should snap to the reference node's right border in X, but Y should be dragged normally
            Assert.AreEqual(referenceNode.layout.xMax, snappedNode.layout.xMin);
            Assert.AreNotEqual(m_SnappingNodePos.x + moveOffset.x, snappedNode.layout.x);
            Assert.AreEqual(m_SnappingNodePos.y + moveOffset.y, snappedNode.layout.y);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementCenterXBorderShouldSnapToLeftBorder()
        {
            // Config
            //  +-------------+
            //  |    Node1    |
            //  +-------------+
            //     +-------+
            //     | Node2 |
            //     +-------+

            m_SnappingNodePos = new Vector2(k_ReferenceNodePos.x + 50, k_ReferenceNodePos.y + 100);

            referenceNodeModel = CreateNode("Node1", k_ReferenceNodePos);
            snappingNodeModel = CreateNode("Node2", m_SnappingNodePos);

            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            // Get the UI nodes
            var snappedNode = snappingNodeModel.GetUI<Node>(graphView);
            var referenceNode = referenceNodeModel.GetUI<Node>(graphView);
            Assert.IsNotNull(snappedNode);
            Assert.IsNotNull(referenceNode);

            // Changing the nodes' sizes to make it easier to test the snapping
            UpdateUINodeSize(ref snappedNode, snappingNodeModel, 100, 100);
            UpdateUINodeSize(ref referenceNode, referenceNodeModel, 100, 200);
            yield return null;

            Vector2 worldNodePos = graphView.contentViewContainer.LocalToWorld(m_SnappingNodePos);
            Vector2 start = worldNodePos + m_SelectionOffset;

            // CENTER TO LEFT BORDER:
            float centerToLeftDistance = snappedNode.layout.center.x - referenceNode.layout.xMin;
            float offSetX = k_SnapDistance - centerToLeftDistance;
            Vector2 moveOffset = new Vector2(offSetX, 10);

            // Move the snapping node.
            helpers.MouseDownEvent(start);
            yield return null;

            Vector2 end = start + moveOffset;
            helpers.MouseDragEvent(start, end);
            yield return null;

            helpers.MouseUpEvent(end);
            yield return null;

            // The snapping node's centerX border should snap to the reference node's left border in X, but Y should be dragged normally
            Assert.AreEqual(referenceNode.layout.xMin, snappedNode.layout.center.x);
            Assert.AreNotEqual(m_SnappingNodePos.x + moveOffset.x, snappedNode.layout.x);
            Assert.AreEqual(m_SnappingNodePos.y + moveOffset.y, snappedNode.layout.y);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementCenterXBorderShouldSnapToRightBorder()
        {
            // Config
            //
            //  +-------------+
            //  |    Node1    |
            //  +-------------+
            //     +-------+
            //     | Node2 |
            //     +-------+

            m_SnappingNodePos = new Vector2(k_ReferenceNodePos.x + 50, k_ReferenceNodePos.y + 100);

            referenceNodeModel = CreateNode("Node1", k_ReferenceNodePos);
            snappingNodeModel = CreateNode("Node2", m_SnappingNodePos);

            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            // Get the UI nodes
            var snappedNode = snappingNodeModel.GetUI<Node>(graphView);
            var referenceNode = referenceNodeModel.GetUI<Node>(graphView);
            Assert.IsNotNull(snappedNode);
            Assert.IsNotNull(referenceNode);

            // Changing the nodes' sizes to make it easier to test the snapping
            UpdateUINodeSize(ref snappedNode, snappingNodeModel, 100, 100);
            UpdateUINodeSize(ref referenceNode, referenceNodeModel, 100, 200);
            yield return null;

            Vector2 worldNodePos = graphView.contentViewContainer.LocalToWorld(m_SnappingNodePos);
            Vector2 start = worldNodePos + m_SelectionOffset;

            // CENTER TO RIGHT BORDER:
            float centerToRightDistance = referenceNode.layout.xMax - snappedNode.layout.center.x;
            float offSetX = k_SnapDistance + centerToRightDistance;
            Vector2 moveOffset = new Vector2(offSetX, 10);

            // Move the snapping node.
            helpers.MouseDownEvent(start);
            yield return null;

            Vector2 end = start + moveOffset;
            helpers.MouseDragEvent(start, end);
            yield return null;

            helpers.MouseUpEvent(end);
            yield return null;

            // The snapping node's centerX border should snap to the reference node's right border in X, but Y should be dragged normally
            Assert.AreEqual(referenceNode.layout.xMax, snappedNode.layout.center.x);
            Assert.AreNotEqual(m_SnappingNodePos.x + moveOffset.x, snappedNode.layout.x);
            Assert.AreEqual(m_SnappingNodePos.y + moveOffset.y, snappedNode.layout.y);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementRightBorderShouldSnapToLeftBorder()
        {
            // Config
            //
            //  +-------------+
            //  |    Node1    |
            //  +-------------+
            //     +-------+
            //     | Node2 |
            //     +-------+

            m_SnappingNodePos = new Vector2(k_ReferenceNodePos.x + 50, k_ReferenceNodePos.y + 100);

            referenceNodeModel = CreateNode("Node1", k_ReferenceNodePos + new Vector2(100, 0));
            snappingNodeModel = CreateNode("Node2", m_SnappingNodePos);

            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            // Get the UI nodes
            var snappedNode = snappingNodeModel.GetUI<Node>(graphView);
            var referenceNode = referenceNodeModel.GetUI<Node>(graphView);
            Assert.IsNotNull(snappedNode);
            Assert.IsNotNull(referenceNode);

            // Changing the nodes' sizes to make it easier to test the snapping
            UpdateUINodeSize(ref snappedNode, snappingNodeModel, 100, 100);
            UpdateUINodeSize(ref referenceNode, referenceNodeModel, 100, 200);
            yield return null;

            Vector2 worldNodePos = graphView.contentViewContainer.LocalToWorld(m_SnappingNodePos);
            Vector2 start = worldNodePos + m_SelectionOffset;

            // RIGHT TO LEFT BORDER:
            float rightToLeftDistance = snappedNode.layout.xMax - referenceNode.layout.xMin;
            float offSetX = k_SnapDistance - rightToLeftDistance;
            Vector2 moveOffset = new Vector2(offSetX, 10);

            // Move the snapping node.
            helpers.MouseDownEvent(start);
            yield return null;

            Vector2 end = start + moveOffset;
            helpers.MouseDragEvent(start, end);
            yield return null;

            helpers.MouseUpEvent(end);
            yield return null;

            // The snapping node's right border should snap to the reference node's left border in X, but Y should be dragged normally
            Assert.AreEqual(referenceNode.layout.xMin, snappedNode.layout.xMax);
            Assert.AreNotEqual(m_SnappingNodePos.x + moveOffset.x, snappedNode.layout.x);
            Assert.AreEqual(m_SnappingNodePos.y + moveOffset.y, snappedNode.layout.y);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementRightBorderShouldSnapToRightBorder()
        {
            // Config
            //  +-------------+
            //  |    Node1    |
            //  +-------------+
            //     +-------+
            //     | Node2 |
            //     +-------+

            m_SnappingNodePos = new Vector2(k_ReferenceNodePos.x + 50, k_ReferenceNodePos.y + 100);

            referenceNodeModel = CreateNode("Node1", k_ReferenceNodePos);
            snappingNodeModel = CreateNode("Node2", m_SnappingNodePos);

            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            // Get the UI nodes
            var snappedNode = snappingNodeModel.GetUI<Node>(graphView);
            var referenceNode = referenceNodeModel.GetUI<Node>(graphView);
            Assert.IsNotNull(snappedNode);
            Assert.IsNotNull(referenceNode);

            // Changing the nodes' sizes to make it easier to test the snapping
            UpdateUINodeSize(ref snappedNode, snappingNodeModel, 100, 100);
            UpdateUINodeSize(ref referenceNode, referenceNodeModel, 100, 200);
            yield return null;

            Vector2 worldNodePos = graphView.contentViewContainer.LocalToWorld(m_SnappingNodePos);
            Vector2 start = worldNodePos + m_SelectionOffset;

            // RIGHT TO RIGHT BORDER:
            float rightToRightDistance = referenceNode.layout.xMax - snappedNode.layout.xMax;
            float offSetX = k_SnapDistance + rightToRightDistance;
            Vector2 moveOffset = new Vector2(offSetX, 10);

            // Move the snapping node.
            helpers.MouseDownEvent(start);
            yield return null;

            Vector2 end = start + moveOffset;
            helpers.MouseDragEvent(start, end);
            yield return null;

            helpers.MouseUpEvent(end);
            yield return null;

            // The snapping node's right border should snap to the reference node's right border in X, but Y should be dragged normally
            Assert.AreEqual(referenceNode.layout.xMax, snappedNode.layout.xMax);
            Assert.AreNotEqual(m_SnappingNodePos.x + moveOffset.x, snappedNode.layout.x);
            Assert.AreEqual(m_SnappingNodePos.y + moveOffset.y, snappedNode.layout.y);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ShouldSnapToMultipleElements()
        {
            // Config
            //           +-------+
            //  +-------+| Node2 |
            //  | Node1 |+-------+
            //  +-------+             O <-- should snap there
            //                        +-------+
            //                        | Node3 |
            //                        +-------+

            m_SnappingNodePos = new Vector2(k_ReferenceNodePos.x + 200, k_ReferenceNodePos.y - 50);

            referenceNodeModel = CreateNode("Node1", k_ReferenceNodePos);
            snappingNodeModel = CreateNode("Node2", m_SnappingNodePos);
            var secondReferenceNodeModel = CreateNode("Node3", k_ReferenceNodePos + new Vector2(400, 200));
            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            // Get the UI nodes
            var snappedNode = snappingNodeModel.GetUI<Node>(graphView);
            var referenceNode = referenceNodeModel.GetUI<Node>(graphView);
            var secondReferenceNode = secondReferenceNodeModel.GetUI<Node>(graphView);
            Assert.IsNotNull(snappedNode);
            Assert.IsNotNull(referenceNode);
            Assert.IsNotNull(secondReferenceNode);

            // Changing the snapping node size to make it easier to test the snapping
            UpdateUINodeSize(ref snappedNode, snappingNodeModel, 100, 100);
            yield return null;

            Vector2 worldNodePos = graphView.contentViewContainer.LocalToWorld(m_SnappingNodePos);
            Vector2 start = worldNodePos + m_SelectionOffset;

            float offsetY = referenceNode.layout.yMax - snappedNode.layout.yMax + k_SnapDistance;
            float offSetX = secondReferenceNode.layout.xMin - snappedNode.layout.xMax + k_SnapDistance;
            Vector2 moveOffset = new Vector2(offSetX, offsetY);

            // Move the snapping node.
            helpers.MouseDownEvent(start);
            yield return null;

            Vector2 end = start + moveOffset;
            helpers.MouseDragEvent(start, end);
            yield return null;

            helpers.MouseUpEvent(end);
            yield return null;

            // The snapping node's bottom right corner should snap to the snapping point O
            Assert.AreEqual(referenceNode.layout.yMax, snappedNode.layout.yMax);
            Assert.AreEqual(secondReferenceNode.layout.xMin, snappedNode.layout.xMax);
            Assert.AreNotEqual(m_SnappingNodePos.x + moveOffset.x, snappedNode.layout.x);
            Assert.AreNotEqual(m_SnappingNodePos.y + moveOffset.y, snappedNode.layout.y);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ShouldNotSnapWhenShiftPressed()
        {
            // Config
            //  +-------------+
            //  |    Node1    |
            //  +-------------+
            //     +-------+
            //     | Node2 |
            //     +-------+

            referenceNodeModel = CreateNode("Node1", k_ReferenceNodePos);

            m_SnappingNodePos = new Vector2(k_ReferenceNodePos.x + 50, k_ReferenceNodePos.y + 100);
            snappingNodeModel = CreateNode("Node2", m_SnappingNodePos);

            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            // Get the UI nodes
            var snappedNode = snappingNodeModel.GetUI<Node>(graphView);
            var referenceNode = referenceNodeModel.GetUI<Node>(graphView);
            Assert.IsNotNull(snappedNode);
            Assert.IsNotNull(referenceNode);

            // Changing the nodes' sizes to make it easier to test the snapping
            UpdateUINodeSize(ref snappedNode, snappingNodeModel, 100, 100);
            UpdateUINodeSize(ref referenceNode, referenceNodeModel, 100, 200);
            yield return null;

            Vector2 worldNodePos = graphView.contentViewContainer.LocalToWorld(m_SnappingNodePos);
            Vector2 start = worldNodePos + m_SelectionOffset;

            float rightToRightDistance = referenceNode.layout.xMax - snappedNode.layout.xMax;
            float offSetX = k_SnapDistance + rightToRightDistance;
            Vector2 moveOffset = new Vector2(offSetX, 10);

            // Move the snapping node.
            helpers.MouseDownEvent(start, MouseButton.LeftMouse, EventModifiers.Shift);
            yield return null;

            Vector2 end = start + moveOffset;
            helpers.MouseDragEvent(start, end, MouseButton.LeftMouse, EventModifiers.Shift);
            yield return null;

            helpers.MouseUpEvent(end, MouseButton.LeftMouse, EventModifiers.Shift);
            yield return null;

            // The snapping node's right border should not snap to the reference node's right border in X: X and Y should be dragged normally
            Assert.AreNotEqual(referenceNode.layout.xMax, snappedNode.layout.xMax);
            Assert.AreEqual(m_SnappingNodePos.x + moveOffset.x, snappedNode.layout.x);
            Assert.AreEqual(m_SnappingNodePos.y + moveOffset.y, snappedNode.layout.y);

            yield return null;
        }

        [UnityTest]
        public IEnumerator PlacematShouldSnap()
        {
            // Config
            //  +---------------+
            //  |     Node1     |
            //  +---------------+
            //     +---------+
            //     | Placemat|
            //     +---------+

            referenceNodeModel = CreateNode("Node1", k_ReferenceNodePos);

            m_SnappingNodePos = new Vector2(k_ReferenceNodePos.x + 50, k_ReferenceNodePos.y + 100);
            var placematModel = CreatePlacemat(new Rect(m_SnappingNodePos, new Vector2(200, 200)), "Placemat");

            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            // Get the UI nodes
            var snappedPlacemat = placematModel.GetUI<Placemat>(graphView);
            var referenceNode = referenceNodeModel.GetUI<Node>(graphView);
            Assert.IsNotNull(snappedPlacemat);
            Assert.IsNotNull(referenceNode);

            // Changing the nodes' sizes to make it easier to test the snapping
            UpdateUINodeSize(ref referenceNode, referenceNodeModel, 100, 300);
            yield return null;

            Vector2 worldNodePos = graphView.contentViewContainer.LocalToWorld(m_SnappingNodePos);
            Vector2 start = worldNodePos + m_SelectionOffset;

            float rightToRightDistance = referenceNode.layout.xMax - snappedPlacemat.layout.xMax;
            float offSetX = k_SnapDistance + rightToRightDistance;
            Vector2 moveOffset = new Vector2(offSetX, 10);

            // Move the snapping placemat
            helpers.MouseDownEvent(start);
            yield return null;

            Vector2 end = start + moveOffset;
            helpers.MouseDragEvent(start, end);
            yield return null;

            helpers.MouseUpEvent(end);
            yield return null;

            // The snapping placemat's right border should snap to the reference node's right border in X: Y should be dragged normally
            Assert.AreEqual(referenceNode.layout.xMax, snappedPlacemat.layout.xMax);
            Assert.AreNotEqual(m_SnappingNodePos.x + moveOffset.x, snappedPlacemat.layout.x);
            Assert.AreEqual(m_SnappingNodePos.y + moveOffset.y, snappedPlacemat.layout.y);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementUnderMouseShouldSnapWhenMultipleSelectedElements()
        {
            // Config
            //   +-------+
            //   | Node1 | +-------+
            //   +-------+ | Node2 | +----------+
            //             +-------+ | Placemat |
            //                       +----------+

            referenceNodeModel = CreateNode("Node1", k_ReferenceNodePos);
            m_SnappingNodePos = new Vector2(k_ReferenceNodePos.x + 200, k_ReferenceNodePos.y + 100);
            snappingNodeModel = CreateNode("Node2", m_SnappingNodePos);

            // Third element
            Vector2 secondSelectedElementPos = new Vector2(m_SnappingNodePos.x + 200, m_SnappingNodePos.y + 100);
            var placematModel = CreatePlacemat(new Rect(secondSelectedElementPos, new Vector2(200, 200)), "Placemat");

            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            Vector2 worldPosNode2 = graphView.contentViewContainer.LocalToWorld(m_SnappingNodePos);
            Vector2 worldPosNode3 = graphView.contentViewContainer.LocalToWorld(secondSelectedElementPos);

            Vector2 selectionPosNode2 = worldPosNode2 + m_SelectionOffset;
            Vector2 selectionPosNode3 = worldPosNode3 + m_SelectionOffset;

            // Select placemat by clicking on it and pressing Ctrl
            helpers.MouseDownEvent(selectionPosNode3, MouseButton.LeftMouse, EventModifiers.Control);
            yield return null;

            helpers.MouseUpEvent(selectionPosNode3, MouseButton.LeftMouse, EventModifiers.Control);
            yield return null;

            // Move mouse to Node2
            helpers.MouseMoveEvent(selectionPosNode3, selectionPosNode2, MouseButton.LeftMouse, EventModifiers.Control);
            yield return null;

            // Select Node2 by clicking on it and pressing Ctrl
            helpers.MouseDownEvent(selectionPosNode2, MouseButton.LeftMouse, EventModifiers.Control);
            yield return null;

            Node node1 = referenceNodeModel.GetUI<Node>(graphView);
            Node node2 = snappingNodeModel.GetUI<Node>(graphView);
            Placemat placemat = placematModel.GetUI<Placemat>(graphView);
            Assert.IsNotNull(node1);
            Assert.IsNotNull(node2);
            Assert.IsNotNull(placemat);

            // Move Node2 toward reference Node1 within the snapping range
            float topToTopDistance = node2.layout.yMin - node1.layout.yMin;
            float offSetY = k_SnapDistance - topToTopDistance;
            Vector2 moveOffset = new Vector2(0, offSetY);
            Vector2 end = selectionPosNode2 + moveOffset;
            helpers.MouseDragEvent(selectionPosNode2, end, MouseButton.LeftMouse, EventModifiers.Control);
            yield return null;

            helpers.MouseUpEvent(end, MouseButton.LeftMouse, EventModifiers.Control);
            yield return null;

            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            // The snapping Node2 top border should snap to Node1's top border
            Assert.AreEqual(node1.layout.yMin, node2.layout.yMin);
            // placemat top border should not snap to Node1's top border
            Assert.AreNotEqual(node1.layout.yMin, placemat.layout.yMin);
            // placemat should have moved by the move offset in x and the same y offset as Node2
            Assert.AreEqual(secondSelectedElementPos.x + moveOffset.x, placemat.layout.x);
            Assert.AreEqual(secondSelectedElementPos.y - topToTopDistance, placemat.layout.y);

            yield return null;
        }

        [UnityTest]
        public IEnumerator PlacematWithCollapsedElementShouldSnap()
        {
            // Config
            //  +---------------+
            //  |     Node1     |
            //  +---------------+
            //     +---------+
            //     | Placemat|
            //     | +-----+ |
            //     | |Node2| |
            //     | +-----+ |
            //     +---------+

            referenceNodeModel = CreateNode("Node1", k_ReferenceNodePos);

            m_SnappingNodePos = new Vector2(k_ReferenceNodePos.x + 50, k_ReferenceNodePos.y + 100);
            var placematModel = CreatePlacemat(new Rect(m_SnappingNodePos, new Vector2(200, 200)), "Placemat");

            var nodeInsidePlacematPos = m_SnappingNodePos + new Vector2(60, 60);
            var nodeInsidePlacematModel = CreateNode("Node2", nodeInsidePlacematPos);
            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            // Get the UI nodes
            var snappedPlacemat = placematModel.GetUI<Placemat>(graphView);
            var referenceNode = referenceNodeModel.GetUI<Node>(graphView);
            var nodeInsidePlacemat = nodeInsidePlacematModel.GetUI<Node>(graphView);

            Assert.IsNotNull(snappedPlacemat);
            Assert.IsNotNull(referenceNode);
            Assert.IsNotNull(nodeInsidePlacemat);

            // Changing the nodes' sizes to make it easier to test the snapping
            UpdateUINodeSize(ref referenceNode, referenceNodeModel, 100, 300);
            UpdateUINodeSize(ref nodeInsidePlacemat, nodeInsidePlacematModel, 100, 100);
            yield return null;

            Vector2 worldNodePos = graphView.contentViewContainer.LocalToWorld(m_SnappingNodePos);
            Vector2 start = worldNodePos + m_SelectionOffset;

            float rightToRightDistance = referenceNode.layout.xMax - snappedPlacemat.layout.xMax;
            float offSetX = k_SnapDistance + rightToRightDistance;
            Vector2 moveOffset = new Vector2(offSetX, 10);

            // Move the snapping placemat
            helpers.MouseDownEvent(start);
            yield return null;

            Vector2 end = start + moveOffset;
            helpers.MouseDragEvent(start, end);
            yield return null;

            helpers.MouseUpEvent(end);
            yield return null;

            // The snapping placemat's right border should snap to the reference node's right border in X: Y should be dragged normally
            Assert.AreEqual(referenceNode.layout.xMax, snappedPlacemat.layout.xMax);
            Assert.AreNotEqual(referenceNode.layout.xMax, nodeInsidePlacemat.layout.xMax);
            Assert.AreEqual(nodeInsidePlacematPos.x + rightToRightDistance, nodeInsidePlacemat.layout.x);
            Assert.AreNotEqual(m_SnappingNodePos.x + moveOffset.x, snappedPlacemat.layout.x);
            Assert.AreEqual(m_SnappingNodePos.y + moveOffset.y, snappedPlacemat.layout.y);

            yield return null;
        }
    }
}
