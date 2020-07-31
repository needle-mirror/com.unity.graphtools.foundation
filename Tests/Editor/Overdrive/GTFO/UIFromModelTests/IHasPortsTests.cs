using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.Helpers;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIFromModelTests
{
    public class IHasPortsTests : BaseTestFixture
    {
        GraphView m_GraphView;
        Helpers.TestStore m_Store;
        GraphModel m_GraphModel;
        Node m_SourceNode;
        Node m_DestinationNode1;
        Node m_DestinationNode2;
        IHasIOPorts m_SourceNodeModel;
        IHasIOPorts m_DestinationNodeModel1;
        IHasIOPorts m_DestinationNodeModel2;
        List<PortModel> m_SourcePortModels;
        List<PortModel> m_DestinationPortModels1;
        List<PortModel> m_DestinationPortModels2;

        [SetUp]
        public new void SetUp()
        {
            m_GraphModel = new GraphModel();
            m_Store = new Helpers.TestStore(new Helpers.TestState(m_GraphModel));
            m_GraphView = new TestGraphView(m_Store) {name = "theView", viewDataKey = "theView"};

            m_GraphView.StretchToParentSize();

            m_Window.rootVisualElement.Add(m_GraphView);

            const int sourceInputPortCount = 0;
            const int sourceOutputPortCount = 2;
            m_SourceNode = CreateNode(sourceInputPortCount, sourceOutputPortCount);
            m_SourceNodeModel = (IHasIOPorts)m_SourceNode.NodeModel;
            m_SourcePortModels = m_SourceNodeModel.OutputPorts.Cast<PortModel>().ToList();

            const int destinationInputPortCount = 1;
            const int destinationOutputPortCount = 0;
            m_DestinationNode1 = CreateNode(destinationInputPortCount, destinationOutputPortCount);
            m_DestinationNodeModel1 = (IHasIOPorts)m_DestinationNode1.NodeModel;
            m_DestinationPortModels1 = m_DestinationNodeModel1.InputPorts.Cast<PortModel>().ToList();

            m_DestinationNode2 = CreateNode(destinationInputPortCount, destinationOutputPortCount);
            m_DestinationNodeModel2 = (IHasIOPorts)m_DestinationNode2.NodeModel;
            m_DestinationPortModels2 = m_DestinationNodeModel2.InputPorts.Cast<PortModel>().ToList();
        }

        Node CreateNode(int inputPortCount, int outputPortCount)
        {
            var nodeModel = new IONodeModel {GraphModel = m_GraphModel};
            var sourceNode = new CollapsibleInOutNode();
            sourceNode.SetupBuildAndUpdate(nodeModel, m_Store, m_GraphView);
            nodeModel.CreatePorts(inputPortCount, outputPortCount);
            return sourceNode;
        }

        Edge CreateEdge(IGTFPortModel to, IGTFPortModel from)
        {
            var model = m_GraphModel.CreateEdge(from, to);
            var edge = new Edge();
            edge.SetupBuildAndUpdate(model, m_Store, m_GraphView);
            return edge;
        }

        [Test]
        public void CanGetPortsWithReorderableEdges()
        {
            m_SourcePortModels[0].HasReorderableEdges = false;
            m_SourcePortModels[1].HasReorderableEdges = true;

            var portsWithReorderableEdges = m_SourceNodeModel.ConnectedPortsWithReorderableEdges().ToList();
            Assert.AreEqual(1, portsWithReorderableEdges.Count);
            Assert.AreEqual(m_SourcePortModels[1], portsWithReorderableEdges[0]);
        }

        [Test]
        public void RevealOrderableEdgeDoesNothingIfPortIsNotReorderableEdgeReady()
        {
            m_SourcePortModels[0].HasReorderableEdges = false;
            var port1Edge1 = CreateEdge(m_SourcePortModels[0], m_DestinationPortModels1[0]);
            var port1Edge2 = CreateEdge(m_SourcePortModels[0], m_DestinationPortModels2[0]);

            m_SourceNodeModel.RevealReorderableEdgesOrder(true);
            Assert.IsTrue(string.IsNullOrEmpty(port1Edge1.EdgeModel.EdgeLabel));
            Assert.IsTrue(string.IsNullOrEmpty(port1Edge2.EdgeModel.EdgeLabel));
        }

        [Test]
        public void RevealOrderableEdgeShowsAndHidesEdgeOrderIfPortIsReorderableEdgeReady()
        {
            m_SourcePortModels[0].HasReorderableEdges = true;
            m_SourcePortModels[1].HasReorderableEdges = true;
            var port1Edge1 = CreateEdge(m_SourcePortModels[0], m_DestinationPortModels1[0]);
            var port1Edge2 = CreateEdge(m_SourcePortModels[0], m_DestinationPortModels2[0]);
            var port2Edge1 = CreateEdge(m_SourcePortModels[1], m_DestinationPortModels1[0]);
            var port2Edge2 = CreateEdge(m_SourcePortModels[1], m_DestinationPortModels2[0]);

            m_SourceNodeModel.RevealReorderableEdgesOrder(true);
            Assert.AreEqual("1", port1Edge1.EdgeModel.EdgeLabel, "Port 1 Edge 1 should have label \"1\" once revealed");
            Assert.AreEqual("2", port1Edge2.EdgeModel.EdgeLabel, "Port 1 Edge 2 should have label \"2\" once revealed");
            Assert.AreEqual("1", port2Edge1.EdgeModel.EdgeLabel, "Port 2 Edge 1 should have label \"1\" once revealed");
            Assert.AreEqual("2", port2Edge2.EdgeModel.EdgeLabel, "Port 2 Edge 2 should have label \"2\" once revealed");

            m_SourceNodeModel.RevealReorderableEdgesOrder(false);
            Assert.IsTrue(string.IsNullOrEmpty(port1Edge1.EdgeModel.EdgeLabel), "Port 1 Edge 1 should have no label when unrevealed");
            Assert.IsTrue(string.IsNullOrEmpty(port1Edge2.EdgeModel.EdgeLabel), "Port 1 Edge 2 should have no label when unrevealed");
            Assert.IsTrue(string.IsNullOrEmpty(port2Edge1.EdgeModel.EdgeLabel), "Port 2 Edge 1 should have no label when unrevealed");
            Assert.IsTrue(string.IsNullOrEmpty(port2Edge2.EdgeModel.EdgeLabel), "Port 2 Edge 2 should have no label when unrevealed");
        }

        [Test]
        public void RevealOrderableEdgeDoesNothingIfPortIsReorderableEdgeReadyWithSingleEdge()
        {
            m_SourcePortModels[0].HasReorderableEdges = true;
            var port1Edge1 = CreateEdge(m_SourcePortModels[0], m_DestinationPortModels1[0]);

            m_SourceNodeModel.RevealReorderableEdgesOrder(true);
            Assert.IsTrue(string.IsNullOrEmpty(port1Edge1.EdgeModel.EdgeLabel));
        }

        [Test]
        public void RevealOrderableEdgeShowsAndHidesEdgeOrderIfPortIsReorderableEdgeReadyOnSpecificEdges()
        {
            m_SourcePortModels[0].HasReorderableEdges = true;
            m_SourcePortModels[1].HasReorderableEdges = true;
            var port1Edge1 = CreateEdge(m_SourcePortModels[0], m_DestinationPortModels1[0]);
            var port1Edge2 = CreateEdge(m_SourcePortModels[0], m_DestinationPortModels2[0]);
            var port2Edge1 = CreateEdge(m_SourcePortModels[1], m_DestinationPortModels1[0]);
            var port2Edge2 = CreateEdge(m_SourcePortModels[1], m_DestinationPortModels2[0]);

            m_SourceNodeModel.RevealReorderableEdgesOrder(true, port1Edge1.EdgeModel);
            Assert.AreEqual("1", port1Edge1.EdgeModel.EdgeLabel, "Port 1 Edge 1 should have label \"1\" once revealed");
            Assert.AreEqual("2", port1Edge2.EdgeModel.EdgeLabel, "Port 1 Edge 2 should have label \"2\" once revealed");
            Assert.IsTrue(string.IsNullOrEmpty(port2Edge1.EdgeModel.EdgeLabel), "Port 2 Edge 1 should have no label since not targeted");
            Assert.IsTrue(string.IsNullOrEmpty(port2Edge2.EdgeModel.EdgeLabel), "Port 2 Edge 2 should have no label since not targeted");

            m_SourceNodeModel.RevealReorderableEdgesOrder(false, port1Edge1.EdgeModel);
            Assert.IsTrue(string.IsNullOrEmpty(port1Edge1.EdgeModel.EdgeLabel), "Port 1 Edge 1 should have no label when unrevealed");
            Assert.IsTrue(string.IsNullOrEmpty(port1Edge2.EdgeModel.EdgeLabel), "Port 1 Edge 2 should have no label when unrevealed");
            Assert.IsTrue(string.IsNullOrEmpty(port2Edge1.EdgeModel.EdgeLabel), "Port 2 Edge 1 should have no label when unrevealed");
            Assert.IsTrue(string.IsNullOrEmpty(port2Edge2.EdgeModel.EdgeLabel), "Port 2 Edge 2 should have no label when unrevealed");
        }

        [UnityTest]
        public IEnumerable RevealOrderableEdgeCalledOnNodeSelection()
        {
            m_SourcePortModels[0].HasReorderableEdges = true;
            m_SourcePortModels[1].HasReorderableEdges = true;
            var port1Edge1 = CreateEdge(m_SourcePortModels[0], m_DestinationPortModels1[0]);
            var port1Edge2 = CreateEdge(m_SourcePortModels[0], m_DestinationPortModels2[0]);
            var port2Edge1 = CreateEdge(m_SourcePortModels[1], m_DestinationPortModels1[0]);
            var port2Edge2 = CreateEdge(m_SourcePortModels[1], m_DestinationPortModels2[0]);

            // Clicking on (i.e. selecting) the node should reveal order for all edges.
            var label = m_SourceNode.Q(Node.k_TitleContainerPartName).Q(EditableLabel.k_LabelName);
            var clickPosition = label.parent.LocalToWorld(label.layout.center);
            Click(label, clickPosition);
            yield return null;
            Assert.AreEqual("1", port1Edge1.EdgeModel.EdgeLabel, "Port 1 Edge 1 should have label \"1\" once revealed");
            Assert.AreEqual("2", port1Edge2.EdgeModel.EdgeLabel, "Port 1 Edge 2 should have label \"2\" once revealed");
            Assert.AreEqual("1", port2Edge1.EdgeModel.EdgeLabel, "Port 2 Edge 1 should have label \"1\" once revealed");
            Assert.AreEqual("2", port2Edge2.EdgeModel.EdgeLabel, "Port 2 Edge 2 should have label \"2\" once revealed");

            // Clicking on (i.e. selecting) an edge should reveal order only for the edges connected to the same port as the clicked edge.
            clickPosition = port1Edge1.EdgeControl.RenderPoints[1];
            Click(m_GraphView, clickPosition);
            yield return null;
            Assert.AreEqual("1", port1Edge1.EdgeModel.EdgeLabel, "Port 1 Edge 1 should have label \"1\" once revealed");
            Assert.AreEqual("2", port1Edge2.EdgeModel.EdgeLabel, "Port 1 Edge 2 should have label \"2\" once revealed");
            Assert.IsTrue(string.IsNullOrEmpty(port2Edge1.EdgeModel.EdgeLabel), "Port 2 Edge 1 should have no label since not targeted");
            Assert.IsTrue(string.IsNullOrEmpty(port2Edge2.EdgeModel.EdgeLabel), "Port 2 Edge 2 should have no label since not targeted");

            // Clicking on empty space (i.e. deselecting everything) should hide all edge orders
            Click(m_GraphView, m_GraphView.layout.min);
            yield return null;
            Assert.IsTrue(string.IsNullOrEmpty(port1Edge1.EdgeModel.EdgeLabel), "Port 1 Edge 1 should have no label when unrevealed");
            Assert.IsTrue(string.IsNullOrEmpty(port1Edge2.EdgeModel.EdgeLabel), "Port 1 Edge 2 should have no label when unrevealed");
            Assert.IsTrue(string.IsNullOrEmpty(port2Edge1.EdgeModel.EdgeLabel), "Port 2 Edge 1 should have no label when unrevealed");
            Assert.IsTrue(string.IsNullOrEmpty(port2Edge2.EdgeModel.EdgeLabel), "Port 2 Edge 2 should have no label when unrevealed");
        }
    }
}
