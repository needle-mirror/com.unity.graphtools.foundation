using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using GraphModel = UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels.GraphModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIFromModelTests
{
    public class PortCommandTests : BaseTestFixture
    {
        GraphView m_GraphView;
        CommandDispatcher m_CommandDispatcher;
        GraphModel m_GraphModel;

        [SetUp]
        public new void SetUp()
        {
            m_GraphModel = new GraphModel();
            m_CommandDispatcher = new CommandDispatcher(new TestGraphToolState(m_Window.GUID, m_GraphModel));
            CommandDispatcherHelper.RegisterDefaultCommandHandlers(m_CommandDispatcher);
            m_GraphView = new GraphView(m_Window, m_CommandDispatcher);

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
            m_CommandDispatcher = null;
            m_GraphView = null;
        }

        [UnityTest]
        public IEnumerator DraggingFromPortCreateGhostEdge()
        {
            var nodeModel = m_GraphModel.CreateNode<SingleOutputNodeModel>();
            var node = new CollapsibleInOutNode();
            node.SetupBuildAndUpdate(nodeModel, m_CommandDispatcher, m_GraphView);
            m_GraphView.AddElement(node);
            yield return null;

            var portModel = nodeModel.Ports.First();
            var port = portModel.GetUI<Port>(m_GraphView);
            Assert.IsNotNull(port);
            Assert.IsNull(port.EdgeConnector.edgeDragHelper.edgeCandidateModel);

            var portConnector = port.Q(PortConnectorPart.connectorUssName);
            var clickPosition = portConnector.parent.LocalToWorld(portConnector.layout.center);
            Vector2 move = new Vector2(0, 100);
            EventHelper.DragToNoRelease(clickPosition, clickPosition + move);
            yield return null;

            // edgeCandidateModel != null is the sign that we have a ghost edge
            Assert.IsNotNull(port.EdgeConnector.edgeDragHelper.edgeCandidateModel);

            EventHelper.MouseUpEvent(clickPosition + move);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ReleasingInPortCallsDelegate()
        {
            var nodeModel1 = m_GraphModel.CreateNode<SingleOutputNodeModel>();
            nodeModel1.Position = Vector2.zero;
            var node1 = new CollapsibleInOutNode();
            node1.SetupBuildAndUpdate(nodeModel1, m_CommandDispatcher, m_GraphView);
            m_GraphView.AddElement(node1);

            var nodeModel2 = m_GraphModel.CreateNode<SingleInputNodeModel>();
            nodeModel2.Position = new Vector2(0, 200);
            var node2 = new CollapsibleInOutNode();
            node2.SetupBuildAndUpdate(nodeModel2, m_CommandDispatcher, m_GraphView);
            m_GraphView.AddElement(node2);
            yield return null;

            var outPortModel = nodeModel1.Ports.First();
            var outPort = outPortModel.GetUI<Port>(m_GraphView);
            Assert.IsNotNull(outPort);

            var inPortModel = nodeModel2.Ports.First();
            var inPort = inPortModel.GetUI<Port>(m_GraphView);
            Assert.IsNotNull(inPort);

            bool insideOutputPortDelegateCalled = false;
            bool insideInputPortDelegateCalled = false;
            bool outsideOutputPortDelegateCalled = false;
            bool outsideInputPortDelegateCalled = false;

            outPort.EdgeConnector.SetDropDelegate((s, e) => insideOutputPortDelegateCalled = true);
            outPort.EdgeConnector.SetDropOutsideDelegate((s, e, v) => outsideOutputPortDelegateCalled = true);
            inPort.EdgeConnector.SetDropDelegate((s, e) => insideInputPortDelegateCalled = true);
            inPort.EdgeConnector.SetDropOutsideDelegate((s, e, v) => outsideInputPortDelegateCalled = true);

            var outPortConnector = outPort.Q(PortConnectorPart.connectorUssName);
            var inPortConnector = inPort.Q(PortConnectorPart.connectorUssName);
            var clickPosition = outPortConnector.parent.LocalToWorld(outPortConnector.layout.center);
            var releasePosition = inPortConnector.parent.LocalToWorld(inPortConnector.layout.center);
            EventHelper.DragTo(clickPosition, releasePosition);
            yield return null;

            Assert.IsFalse(insideInputPortDelegateCalled);
            Assert.IsTrue(insideOutputPortDelegateCalled);
            Assert.IsFalse(outsideInputPortDelegateCalled);
            Assert.IsFalse(outsideOutputPortDelegateCalled);
        }

        [UnityTest]
        public IEnumerator ReleasingOutsidePortCallsDelegate()
        {
            var nodeModel1 = m_GraphModel.CreateNode<SingleOutputNodeModel>();
            nodeModel1.Position = Vector2.zero;
            var node1 = new CollapsibleInOutNode();
            node1.SetupBuildAndUpdate(nodeModel1, m_CommandDispatcher, m_GraphView);
            m_GraphView.AddElement(node1);

            var nodeModel2 = m_GraphModel.CreateNode<SingleInputNodeModel>();
            nodeModel2.Position = new Vector2(0, 200);
            var node2 = new CollapsibleInOutNode();
            node2.SetupBuildAndUpdate(nodeModel2, m_CommandDispatcher, m_GraphView);
            m_GraphView.AddElement(node2);
            yield return null;

            var outPortModel = nodeModel1.Ports.First();
            var outPort = outPortModel.GetUI<Port>(m_GraphView);
            Assert.IsNotNull(outPort);

            var inPortModel = nodeModel2.Ports.First();
            var inPort = inPortModel.GetUI<Port>(m_GraphView);
            Assert.IsNotNull(inPort);

            bool insideOutputPortDelegateCalled = false;
            bool insideInputPortDelegateCalled = false;
            bool outsideOutputPortDelegateCalled = false;
            bool outsideInputPortDelegateCalled = false;

            outPort.EdgeConnector.SetDropDelegate((s, e) => insideOutputPortDelegateCalled = true);
            outPort.EdgeConnector.SetDropOutsideDelegate((s, e, v) => outsideOutputPortDelegateCalled = true);
            inPort.EdgeConnector.SetDropDelegate((s, e) => insideInputPortDelegateCalled = true);
            inPort.EdgeConnector.SetDropOutsideDelegate((s, e, v) => outsideInputPortDelegateCalled = true);

            var outPortConnector = outPort.Q(PortConnectorPart.connectorUssName);
            var inPortConnector = inPort.Q(PortConnectorPart.connectorUssName);
            var clickPosition = outPortConnector.parent.LocalToWorld(outPortConnector.layout.center);
            var releasePosition = inPortConnector.parent.LocalToWorld(inPortConnector.layout.center);
            EventHelper.DragTo(clickPosition, releasePosition + 400 * Vector2.down);
            yield return null;

            Assert.IsFalse(insideInputPortDelegateCalled);
            Assert.IsFalse(insideOutputPortDelegateCalled);
            Assert.IsFalse(outsideInputPortDelegateCalled);
            Assert.IsTrue(outsideOutputPortDelegateCalled);
        }
    }
}
