using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements.Utilities;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    public class GraphElementEdgeInteractionsTests : GraphViewTester
    {
        BasicNodeModel firstNode { get; set; }
        BasicNodeModel secondNode { get; set; }
        BasicPortModel startPort { get; set; }
        BasicPortModel endPort { get; set; }
        BasicPortModel startPortTwo { get; set; }
        BasicPortModel endPortTwo { get; set; }

        static readonly Vector2 k_FirstNodePosition = new Vector2(0, 0);
        static readonly Vector2 k_SecondNodePosition = new Vector2(400, 0);
        const float k_EdgeSelectionOffset = 20.0f;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            firstNode = CreateNode("First Node", new Vector2(0, 0));
            startPort = firstNode.AddPort(Orientation.Horizontal, Direction.Output, PortCapacity.Multi, typeof(float));
            startPortTwo = firstNode.AddPort(Orientation.Horizontal, Direction.Output, PortCapacity.Multi, typeof(float));

            secondNode = CreateNode("Second Node", new Vector2(400, 0));
            endPort = secondNode.AddPort(Orientation.Horizontal, Direction.Input, PortCapacity.Single, typeof(float));
            endPortTwo = secondNode.AddPort(Orientation.Horizontal, Direction.Input, PortCapacity.Single, typeof(float));
        }

        [UnityTest]
        public IEnumerator MixedOrientationEdges()
        {
            BasicNodeModel horizontalNode = CreateNode("Horizontal Node", new Vector2(100, 200));
            BasicPortModel hInPort = horizontalNode.AddPort(Orientation.Horizontal, Direction.Input, PortCapacity.Single, typeof(float));
            BasicPortModel hOutPort = horizontalNode.AddPort(Orientation.Horizontal, Direction.Output, PortCapacity.Multi, typeof(float));

            BasicNodeModel verticalNode = CreateNode("Horizontal Node", new Vector2(500, 100));
            BasicPortModel vInPort = verticalNode.AddPort(Orientation.Vertical, Direction.Input, PortCapacity.Single, typeof(float));
            BasicPortModel vOutPort = verticalNode.AddPort(Orientation.Vertical, Direction.Output, PortCapacity.Multi, typeof(float));

            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            var actions = ConnectPorts(hOutPort, vInPort);
            while (actions.MoveNext())
            {
                yield return null;
            }

            BasicEdgeModel edgeModel = hOutPort.GetConnectedEdges().First() as BasicEdgeModel;
            Assert.IsNotNull(edgeModel);

            Port outputPort = hOutPort.GetUI<Port>(graphView);
            Port inputPort = vInPort.GetUI<Port>(graphView);

            Assert.IsNotNull(outputPort);
            Assert.IsNotNull(inputPort);

            Edge edge = edgeModel.GetUI<Edge>(graphView);
            Assert.AreEqual(inputPort.PortModel, edge.Input);
            Assert.AreEqual(outputPort.PortModel, edge.Output);
            Assert.AreEqual(Orientation.Vertical, edge.EdgeControl.InputOrientation);
            Assert.AreEqual(Orientation.Horizontal, edge.EdgeControl.OutputOrientation);

            actions = ConnectPorts(vOutPort, hInPort);
            while (actions.MoveNext())
            {
                yield return null;
            }

            edgeModel = vOutPort.GetConnectedEdges().First() as BasicEdgeModel;
            Assert.IsNotNull(edgeModel);

            outputPort = vOutPort.GetUI<Port>(graphView);
            inputPort = hInPort.GetUI<Port>(graphView);

            Assert.IsNotNull(outputPort);
            Assert.IsNotNull(inputPort);

            edge = edgeModel.GetUI<Edge>(graphView);
            Assert.AreEqual(inputPort.PortModel, edge.Input);
            Assert.AreEqual(outputPort.PortModel, edge.Output);
            Assert.AreEqual(Orientation.Horizontal, edge.EdgeControl.InputOrientation);
            Assert.AreEqual(Orientation.Vertical, edge.EdgeControl.OutputOrientation);
        }

        [UnityTest]
        public IEnumerator EdgeConnectOnSinglePortOutputToInputWorks()
        {
            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            // We start without any connection
            Assert.IsFalse(startPort.IsConnected());
            Assert.IsFalse(endPort.IsConnected());

            var actions = ConnectPorts(startPort, endPort);
            while (actions.MoveNext())
            {
                yield return null;
            }

            // Check that the edge exists and that it connects the two ports.
            Assert.IsTrue(startPort.IsConnected());
            Assert.IsTrue(endPort.IsConnected());
            Assert.IsTrue(startPort.IsConnectedTo(endPort));

            var edge = startPort.GetConnectedEdges().First();
            Assert.IsNotNull(edge);

            var edgeUI = edge.GetUI<Edge>(graphView);
            Assert.IsNotNull(edgeUI);
            Assert.IsNotNull(edgeUI.parent);
        }

        // TODO Add Test multi port works
        // TODO Add Test multi connection to single port replaces connection
        // TODO Add Test disallow multiple edges on same multiport pairs (e.g. multiple edges between output A and input B)

        [UnityTest]
        public IEnumerator EdgeConnectOnSinglePortInputToOutputWorks()
        {
            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            // We start without any connection
            Assert.IsFalse(startPort.IsConnected());
            Assert.IsFalse(endPort.IsConnected());

            var actions = ConnectPorts(endPort, startPort);
            while (actions.MoveNext())
            {
                yield return null;
            }

            // Check that the edge exists and that it connects the two ports.
            Assert.IsTrue(startPort.IsConnected());
            Assert.IsTrue(endPort.IsConnected());
            Assert.IsTrue(startPort.IsConnectedTo(endPort));

            var edge = startPort.GetConnectedEdges().First();
            Assert.IsNotNull(edge);

            var edgeUI = edge.GetUI<Edge>(graphView);
            Assert.IsNotNull(edgeUI);
            Assert.IsNotNull(edgeUI.parent);
        }

        [UnityTest]
        public IEnumerator EdgeDisconnectInputWorks()
        {
            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            var actions = ConnectPorts(startPort, endPort);
            while (actions.MoveNext())
            {
                yield return null;
            }

            var startPortUI = startPort.GetUI<Port>(graphView);
            var endPortUI = endPort.GetUI<Port>(graphView);
            Assert.IsNotNull(startPortUI);
            Assert.IsNotNull(endPortUI);
            var startPortPosition = startPortUI.GetGlobalCenter();
            var endPortPosition = endPortUI.GetGlobalCenter();

            var edgeModel = startPort.GetConnectedEdges().First();
            var edge = edgeModel.GetUI<Edge>(graphView);
            Assert.IsNotNull(edge);
            VisualElement edgeParent = edge.parent;

            // Mouse press on the right half of the edge
            var edgeRightSegmentPos = new Vector2(endPortPosition.x - k_EdgeSelectionOffset, endPortPosition.y);
            helpers.MouseDownEvent(edgeRightSegmentPos);
            yield return null;

            // Mouse move to the empty area while holding CTRL.
            var emptyAreaPos = new Vector2(startPortPosition.x + (endPortPosition.x - startPortPosition.x) / 2, endPortPosition.y);
            helpers.MouseDragEvent(edgeRightSegmentPos, emptyAreaPos, MouseButton.LeftMouse, EventModifiers.Control);
            yield return null;

            Assert.AreEqual(startPort, edge.Output);
            Assert.IsNotNull(edge.parent);
            Assert.AreEqual(edgeParent, edge.parent);

            // Mouse release on the empty area
            helpers.MouseUpEvent(emptyAreaPos);
            yield return null;

            Assert.IsEmpty(startPort.GetConnectedEdges());
        }

        [UnityTest]
        public IEnumerator EdgeDisconnectOutputWorks()
        {
            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            var startPortUI = startPort.GetUI<Port>(graphView);
            var endPortUI = endPort.GetUI<Port>(graphView);

            float startPortX = startPortUI.GetGlobalCenter().x;
            float startPortY = startPortUI.GetGlobalCenter().y;
            float endPortX = endPortUI.GetGlobalCenter().x;

            // Create the edge to be tested.
            var actions = ConnectPorts(startPort, endPort);
            while (actions.MoveNext())
            {
                yield return null;
            }

            var edge = startPort.GetConnectedEdges().First().GetUI<Edge>(graphView);
            VisualElement edgeParent = edge.parent;

            // Mouse press on the left half of the edge
            var edgeLeftSegmentPos = new Vector2(startPortX + k_EdgeSelectionOffset, startPortY);
            helpers.MouseDownEvent(edgeLeftSegmentPos);
            yield return null;

            // Mouse move to the empty area while holding CTRL.
            var emptyAreaPos = new Vector2(startPortX + (endPortX - startPortX) / 2, startPortY);
            helpers.MouseDragEvent(edgeLeftSegmentPos, emptyAreaPos, MouseButton.LeftMouse, EventModifiers.Control);
            yield return null;

            Assert.AreEqual(endPort, edge.Input);
            Assert.IsNotNull(edge.parent);
            Assert.AreEqual(edgeParent, edge.parent);

            // Mouse release on the empty area
            helpers.MouseUpEvent(emptyAreaPos);
            yield return null;

            Assert.IsEmpty(startPort.GetConnectedEdges());
        }

        [UnityTest]
        public IEnumerator EdgeReconnectInputWorks()
        {
            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            var endPortUI = endPort.GetUI<Port>(graphView);

            float endPortX = endPortUI.GetGlobalCenter().x;
            float endPortY = endPortUI.GetGlobalCenter().y;

            // Create the edge to be tested.
            var actions = ConnectPorts(startPort, endPort);
            while (actions.MoveNext())
            {
                yield return null;
            }

            var edge = startPort.GetConnectedEdges().First().GetUI<Edge>(graphView);

            // Allow one frame for the edge to be placed onto a layer
            yield return null;

            // Allow one frame for the edge to be rendered and process its layout a first time
            yield return null;

            VisualElement edgeParent = edge.parent;

            // Mouse press on the right half of the edge
            var edgeRightSegmentPos = new Vector2(endPortX - k_EdgeSelectionOffset, endPortY);
            helpers.MouseDownEvent(edgeRightSegmentPos);
            yield return null;

            // Mouse move to the second port while holding CTRL.
            var endPortTwoUI = endPortTwo.GetUI<Port>(graphView);
            var portTwoAreaPos = endPortTwoUI.GetGlobalCenter();
            helpers.MouseDragEvent(edgeRightSegmentPos, portTwoAreaPos, MouseButton.LeftMouse, EventModifiers.Control);
            yield return null;

            Assert.AreEqual(startPort, edge.Output);
            Assert.IsNotNull(edge.parent);
            Assert.AreEqual(edgeParent, edge.parent);

            // Mouse release on the port area
            helpers.MouseUpEvent(portTwoAreaPos);
            yield return null;

            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            edge = startPort.GetConnectedEdges().First().GetUI<Edge>(graphView);

            Assert.AreEqual(startPort, edge.Output);
            Assert.AreEqual(endPortTwo, edge.Input);
            Assert.IsNotNull(edge.parent);
            Assert.AreEqual(edgeParent, edge.parent);
        }

        [UnityTest]
        public IEnumerator EdgeReconnectOutputWorks()
        {
            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            var startPortUI = startPort.GetUI<Port>(graphView);

            float startPortX = startPortUI.GetGlobalCenter().x;
            float startPortY = startPortUI.GetGlobalCenter().y;

            // Create the edge to be tested.
            var actions = ConnectPorts(startPort, endPort);
            while (actions.MoveNext())
            {
                yield return null;
            }

            var edge = startPort.GetConnectedEdges().First().GetUI<Edge>(graphView);
            VisualElement edgeParent = edge.parent;

            // Mouse press on the left half of the edge
            var edgeLeftSegmentPos = new Vector2(startPortX + k_EdgeSelectionOffset, startPortY);
            helpers.MouseDownEvent(edgeLeftSegmentPos);
            yield return null;

            // Mouse move to the second port while holding CTRL.
            var startPortTwoUI = startPortTwo.GetUI<Port>(graphView);
            var portTwoAreaPos = startPortTwoUI.GetGlobalCenter();
            helpers.MouseDragEvent(edgeLeftSegmentPos, portTwoAreaPos, MouseButton.LeftMouse, EventModifiers.Control);
            yield return null;

            Assert.AreEqual(endPort, edge.Input);
            Assert.IsNotNull(edge.parent);
            Assert.AreEqual(edgeParent, edge.parent);

            // Mouse release on the empty area
            helpers.MouseUpEvent(portTwoAreaPos);
            yield return null;

            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            edge = startPortTwo.GetConnectedEdges().First().GetUI<Edge>(graphView);

            Assert.AreEqual(startPortTwo, edge.Output);
            Assert.AreEqual(endPort, edge.Input);
            Assert.IsNotNull(edge.parent);
            Assert.AreEqual(edgeParent, edge.parent);
        }

        [UnityTest]
        public IEnumerator CanCancelEdgeManipulationOnInput()
        {
            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            var startPortUI = startPort.GetUI<Port>(graphView);
            var endPortUI = endPort.GetUI<Port>(graphView);

            float startPortX = startPortUI.GetGlobalCenter().x;
            float endPortX = endPortUI.GetGlobalCenter().x;
            float endPortY = endPortUI.GetGlobalCenter().y;

            // Create the edge to be tested.
            var actions = ConnectPorts(startPort, endPort);
            while (actions.MoveNext())
            {
                yield return null;
            }

            var edge = startPort.GetConnectedEdges().First().GetUI<Edge>(graphView);

            VisualElement edgeParent = edge.parent;

            // Mouse press on the right half of the edge
            var edgeRightSegmentPos = new Vector2(endPortX - k_EdgeSelectionOffset, endPortY);
            helpers.MouseDownEvent(edgeRightSegmentPos);
            yield return null;

            // Mouse move to the empty area while holding CTRL.
            var emptyAreaPos = new Vector2(startPortX + (endPortX - startPortX) / 2, endPortY);
            helpers.MouseDragEvent(edgeRightSegmentPos, emptyAreaPos, MouseButton.LeftMouse, EventModifiers.Control);
            yield return null;

            Assert.AreEqual(startPort, edge.Output);
            Assert.IsNotNull(edge.parent);
            Assert.AreEqual(edgeParent, edge.parent);

            // Key down with ESC key
            helpers.KeyDownEvent(KeyCode.Escape);
            yield return null;

            Assert.AreEqual(startPort, edge.Output);
            Assert.AreEqual(endPort, edge.Input);
            Assert.IsNotNull(edge.parent);
            Assert.AreEqual(edgeParent, edge.parent);

            // Key up to keep the event flow consistent
            helpers.KeyUpEvent(KeyCode.Escape);
            yield return null;

            // Mouse release on the empty area
            helpers.MouseUpEvent(emptyAreaPos);
            yield return null;

            Assert.AreEqual(startPort, edge.Output);
            Assert.AreEqual(endPort, edge.Input);
            Assert.IsNotNull(edge.parent);
            Assert.AreEqual(edgeParent, edge.parent);
        }

        [UnityTest]
        public IEnumerator CanCancelEdgeManipulationOnOutput()
        {
            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            var startPortUI = startPort.GetUI<Port>(graphView);
            var endPortUI = endPort.GetUI<Port>(graphView);

            float startPortX = startPortUI.GetGlobalCenter().x;
            float startPortY = startPortUI.GetGlobalCenter().y;
            float endPortX = endPortUI.GetGlobalCenter().x;

            // Create the edge to be tested.
            var actions = ConnectPorts(startPort, endPort);
            while (actions.MoveNext())
            {
                yield return null;
            }

            var edge = startPort.GetConnectedEdges().First().GetUI<Edge>(graphView);

            VisualElement edgeParent = edge.parent;

            // Mouse press on the left half of the edge
            var edgeLeftSegmentPos = new Vector2(startPortX + k_EdgeSelectionOffset, startPortY);
            helpers.MouseDownEvent(edgeLeftSegmentPos);
            yield return null;

            // Mouse move to the empty area while holding CTRL.
            var emptyAreaPos = new Vector2(startPortX + (endPortX - startPortX) / 2, startPortY);
            helpers.MouseDragEvent(edgeLeftSegmentPos, emptyAreaPos, MouseButton.LeftMouse, EventModifiers.Control);
            yield return null;

            Assert.AreEqual(endPort, edge.Input);
            Assert.IsNotNull(edge.parent);
            Assert.AreEqual(edgeParent, edge.parent);

            // Key down with ESC key
            helpers.KeyDownEvent(KeyCode.Escape);
            yield return null;

            Assert.AreEqual(startPort, edge.Output);
            Assert.AreEqual(endPort, edge.Input);
            Assert.IsNotNull(edge.parent);
            Assert.AreEqual(edgeParent, edge.parent);

            // Key up to keep the event flow consistent
            helpers.KeyUpEvent(KeyCode.Escape);
            yield return null;

            // Mouse release on the empty area
            helpers.MouseUpEvent(emptyAreaPos);
            yield return null;

            Assert.AreEqual(startPort, edge.Output);
            Assert.AreEqual(endPort, edge.Input);
            Assert.IsNotNull(edge.parent);
            Assert.AreEqual(edgeParent, edge.parent);
        }

        [UnityTest]
        public IEnumerator EdgeConnectionUnderThresholdDistanceNotEffective()
        {
            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            Port startPortUI = startPort.GetUI<Port>(graphView);
            var startPos = startPortUI.GetGlobalCenter();
            helpers.DragTo(startPos, startPos + new Vector3(EdgeConnector.k_ConnectionDistanceThreshold / 2f, 0f, 0f));

            yield return null;

            Assert.AreEqual(0, secondNode.GetInputPorts().First().GetConnectedEdges().Count());

            yield return null;
        }
    }
}
