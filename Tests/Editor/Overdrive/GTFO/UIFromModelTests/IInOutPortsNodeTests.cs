using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIFromModelTests
{
    public class IHasPortsTests : BaseTestFixture
    {
        GraphView m_GraphView;
        CommandDispatcher m_CommandDispatcher;
        GraphModel m_GraphModel;
        Node m_SourceNode;
        Node m_DestinationNode1;
        Node m_DestinationNode2;
        IInOutPortsNode m_SourceNodeModel;
        IInOutPortsNode m_DestinationNodeModel1;
        IInOutPortsNode m_DestinationNodeModel2;
        List<PortModel> m_SourcePortModels;
        List<PortModel> m_DestinationPortModels1;
        List<PortModel> m_DestinationPortModels2;

        [SetUp]
        public new void SetUp()
        {
            m_GraphModel = new GraphModel();
            m_CommandDispatcher = new CommandDispatcher(new TestGraphToolState(m_Window.GUID, m_GraphModel));
            CommandDispatcherHelper.RegisterDefaultCommandHandlers(m_CommandDispatcher);
            m_GraphView = new GraphView(m_Window, m_CommandDispatcher) {name = "theView", viewDataKey = "theView"};

            m_GraphView.StretchToParentSize();

            m_Window.rootVisualElement.Add(m_GraphView);

            const int sourceInputPortCount = 0;
            const int sourceOutputPortCount = 2;
            m_SourceNode = CreateNode(sourceInputPortCount, sourceOutputPortCount);
            m_SourceNodeModel = (IInOutPortsNode)m_SourceNode.NodeModel;
            m_SourcePortModels = m_SourceNodeModel.GetOutputPorts().Cast<PortModel>().ToList();

            const int destinationInputPortCount = 1;
            const int destinationOutputPortCount = 0;
            m_DestinationNode1 = CreateNode(destinationInputPortCount, destinationOutputPortCount);
            m_DestinationNodeModel1 = (IInOutPortsNode)m_DestinationNode1.NodeModel;
            m_DestinationPortModels1 = m_DestinationNodeModel1.GetInputPorts().Cast<PortModel>().ToList();

            m_DestinationNode2 = CreateNode(destinationInputPortCount, destinationOutputPortCount);
            m_DestinationNodeModel2 = (IInOutPortsNode)m_DestinationNode2.NodeModel;
            m_DestinationPortModels2 = m_DestinationNodeModel2.GetInputPorts().Cast<PortModel>().ToList();
        }

        [TearDown]
        public new void TearDown()
        {
            m_Window.rootVisualElement.Remove(m_GraphView);
            m_GraphModel = null;
            m_CommandDispatcher = null;
            m_GraphView = null;
        }

        Node CreateNode(int inputPortCount, int outputPortCount)
        {
            var nodeModel = m_GraphModel.CreateNode<IONodeModel>(preDefineSetup: model =>
            {
                model.InputCount = inputPortCount;
                model.OuputCount = outputPortCount;
            });
            var sourceNode = new CollapsibleInOutNode();
            sourceNode.SetupBuildAndUpdate(nodeModel, m_CommandDispatcher, m_GraphView);
            return sourceNode;
        }

        Edge CreateEdge(IPortModel to, IPortModel from)
        {
            var model = m_GraphModel.CreateEdge(from, to);
            var edge = new Edge();
            edge.SetupBuildAndUpdate(model, m_CommandDispatcher, m_GraphView);
            return edge;
        }

        [Test]
        public void CanGetPortsWithReorderableEdges()
        {
            m_SourcePortModels[0].SetReorderable(false);
            m_SourcePortModels[1].SetReorderable(true);

            CreateEdge(m_SourcePortModels[0], m_DestinationPortModels1[0]);
            CreateEdge(m_SourcePortModels[1], m_DestinationPortModels1[0]);

            var portsWithReorderableEdges = m_SourceNodeModel.ConnectedPortsWithReorderableEdges().ToList();
            Assert.AreEqual(1, portsWithReorderableEdges.Count);
            Assert.AreEqual(m_SourcePortModels[1], portsWithReorderableEdges[0]);
        }

        [Test]
        public void RevealOrderableEdgeDoesNothingIfPortIsNotReorderableEdgeReady()
        {
            m_SourcePortModels[0].SetReorderable(false);
            var port1Edge1 = CreateEdge(m_SourcePortModels[0], m_DestinationPortModels1[0]);
            var port1Edge2 = CreateEdge(m_SourcePortModels[0], m_DestinationPortModels2[0]);

            m_SourceNodeModel.RevealReorderableEdgesOrder(true);
            Assert.IsTrue(string.IsNullOrEmpty(port1Edge1.EdgeModel.EdgeLabel));
            Assert.IsTrue(string.IsNullOrEmpty(port1Edge2.EdgeModel.EdgeLabel));
        }

        [Test]
        public void RevealOrderableEdgeShowsAndHidesEdgeOrderIfPortIsReorderableEdgeReady()
        {
            m_SourcePortModels[0].SetReorderable(true);
            m_SourcePortModels[1].SetReorderable(true);
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
            m_SourcePortModels[0].SetReorderable(true);
            var port1Edge1 = CreateEdge(m_SourcePortModels[0], m_DestinationPortModels1[0]);

            m_SourceNodeModel.RevealReorderableEdgesOrder(true);
            Assert.IsTrue(string.IsNullOrEmpty(port1Edge1.EdgeModel.EdgeLabel));
        }

        [Test]
        public void RevealOrderableEdgeShowsAndHidesEdgeOrderIfPortIsReorderableEdgeReadyOnSpecificEdges()
        {
            m_SourcePortModels[0].SetReorderable(true);
            m_SourcePortModels[1].SetReorderable(true);
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

        [Test]
        public void RevealOrderableEdgeCalledOnNodeSelection()
        {
            m_SourcePortModels[0].SetReorderable(true);
            m_SourcePortModels[1].SetReorderable(true);
            var port1Edge1 = CreateEdge(m_SourcePortModels[0], m_DestinationPortModels1[0]);
            var port1Edge2 = CreateEdge(m_SourcePortModels[0], m_DestinationPortModels2[0]);
            var port2Edge1 = CreateEdge(m_SourcePortModels[1], m_DestinationPortModels1[0]);
            var port2Edge2 = CreateEdge(m_SourcePortModels[1], m_DestinationPortModels2[0]);

            m_GraphView.AddToSelection(m_SourceNode);
            Assert.AreEqual("1", port1Edge1.EdgeModel.EdgeLabel, "Port 1 Edge 1 should have label \"1\" once revealed");
            Assert.AreEqual("2", port1Edge2.EdgeModel.EdgeLabel, "Port 1 Edge 2 should have label \"2\" once revealed");
            Assert.AreEqual("1", port2Edge1.EdgeModel.EdgeLabel, "Port 2 Edge 1 should have label \"1\" once revealed");
            Assert.AreEqual("2", port2Edge2.EdgeModel.EdgeLabel, "Port 2 Edge 2 should have label \"2\" once revealed");

            m_GraphView.ClearSelection();
            m_GraphView.AddToSelection(port1Edge1);
            Assert.AreEqual("1", port1Edge1.EdgeModel.EdgeLabel, "Port 1 Edge 1 should have label \"1\" once revealed");
            Assert.AreEqual("2", port1Edge2.EdgeModel.EdgeLabel, "Port 1 Edge 2 should have label \"2\" once revealed");
            Assert.IsTrue(string.IsNullOrEmpty(port2Edge1.EdgeModel.EdgeLabel), "Port 2 Edge 1 should have no label since not targeted");
            Assert.IsTrue(string.IsNullOrEmpty(port2Edge2.EdgeModel.EdgeLabel), "Port 2 Edge 2 should have no label since not targeted");

            m_GraphView.ClearSelection();
            Assert.IsTrue(string.IsNullOrEmpty(port1Edge1.EdgeModel.EdgeLabel), "Port 1 Edge 1 should have no label when unrevealed");
            Assert.IsTrue(string.IsNullOrEmpty(port1Edge2.EdgeModel.EdgeLabel), "Port 1 Edge 2 should have no label when unrevealed");
            Assert.IsTrue(string.IsNullOrEmpty(port2Edge1.EdgeModel.EdgeLabel), "Port 2 Edge 1 should have no label when unrevealed");
            Assert.IsTrue(string.IsNullOrEmpty(port2Edge2.EdgeModel.EdgeLabel), "Port 2 Edge 2 should have no label when unrevealed");
        }
    }
}
