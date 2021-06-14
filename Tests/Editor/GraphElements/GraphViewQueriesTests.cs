using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class GraphViewQueriesTests : GraphViewTester
    {
        IInputOutputPortsNodeModel m_Node1;
        IInputOutputPortsNodeModel m_Node2;
        IInputOutputPortsNodeModel m_Node3;
        IInputOutputPortsNodeModel m_Node4;
        IEdgeModel m_Edge1;
        IEdgeModel m_Edge2;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            m_Node1 = CreateNode("Node 1", new Vector2(100, 100), 2, 2);
            m_Node2 = CreateNode("Node 2", new Vector2(200, 200), 2, 2);
            m_Node3 = CreateNode("Node 3", new Vector2(400, 400));
            m_Node4 = CreateNode("Node 4", new Vector2(500, 500));
            m_Edge1 = GraphModel.CreateEdge(m_Node1.GetInputPorts().First(), m_Node2.GetOutputPorts().First());
            m_Edge2 = GraphModel.CreateEdge(m_Node1.GetInputPorts().First(), m_Node2.GetOutputPorts().ElementAt(1));
        }

        IEnumerable<GraphElement> GetElements<T>() where T : GraphElement
        {
            GraphElement e = m_Node1.GetUI<T>(graphView);
            if (e != null) yield return e;

            e = m_Node2.GetUI<T>(graphView);
            if (e != null) yield return e;

            e = m_Node3.GetUI<T>(graphView);
            if (e != null) yield return e;

            e = m_Node4.GetUI<T>(graphView);
            if (e != null) yield return e;

            e = m_Edge1.GetUI<T>(graphView);
            if (e != null) yield return e;

            e = m_Edge2.GetUI<T>(graphView);
            if (e != null) yield return e;
        }

        [Test]
        public void QueryAllElements()
        {
            graphView.RebuildUI(GraphModel, CommandDispatcher);
            var allElements = graphView.GraphElements.ToList();

            Assert.AreEqual(6, allElements.Count);

            foreach (var e in GetElements<GraphElement>())
            {
                Assert.IsTrue(allElements.Contains(e));
            }
        }

        [Test]
        public void QueryAllNodes()
        {
            graphView.RebuildUI(GraphModel, CommandDispatcher);
            List<Node> allNodes = graphView.Nodes.ToList();

            Assert.AreEqual(4, allNodes.Count);

            foreach (var e in GetElements<Node>())
            {
                Assert.IsTrue(allNodes.Contains(e));
            }
        }

        [Test]
        public void QueryAllEdges()
        {
            graphView.RebuildUI(GraphModel, CommandDispatcher);
            List<Edge> allEdges = graphView.Edges.ToList();

            Assert.AreEqual(2, allEdges.Count);

            foreach (var e in GetElements<Edge>())
            {
                Assert.IsTrue(allEdges.Contains(e));
            }
        }

        [Test]
        public void QueryAllPorts()
        {
            graphView.RebuildUI(GraphModel, CommandDispatcher);
            Assert.AreEqual(8, graphView.Ports.ToList().Count);
        }
    }
}
