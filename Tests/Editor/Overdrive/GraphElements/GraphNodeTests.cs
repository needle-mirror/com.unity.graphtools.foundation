using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements.Utilities;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    public class GraphNodeTests : GraphViewTester
    {
        BasicNodeModel m_Node1;
        BasicNodeModel m_Node2;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            m_Node1 = CreateNode("Node 1", new Vector2(0, 0), 0, 1);
            m_Node2 = CreateNode("Node 2", new Vector2(300, 300), 1);

            // Add the minimap.
            var miniMap = new MiniMap();
            miniMap.SetPosition(new Rect(10, 100, 100, 100));
            miniMap.maxWidth = 100;
            miniMap.maxHeight = 100;
            graphView.Add(miniMap);
        }

        [Test]
        public void CollapseButtonOnlyEnabledWhenNodeHasUnconnectedPorts()
        {
            graphView.RebuildUI(GraphModel, Store);
            List<Node> nodeList = graphView.nodes.ToList();

            // Nothing is connected. The collapse button should be enabled.
            Assert.AreEqual(2, nodeList.Count);
            foreach (Node node in nodeList)
            {
                VisualElement collapseButton = node.Q<VisualElement>(name: "collapse-button");
                Assert.False(collapseButton.GetDisabledPseudoState());
            }

            var edge = GraphModel.CreateEdge(m_Node1.OutputPorts.First(), m_Node2.InputPorts.First());
            graphView.RebuildUI(GraphModel, Store);
            nodeList = graphView.nodes.ToList();

            // Ports are connected. The collapse button should be disabled.
            Assert.AreEqual(2, nodeList.Count);
            foreach (Node node in nodeList)
            {
                VisualElement collapseButton = node.Q<VisualElement>(name: "collapse-button");
                Assert.True(collapseButton.GetDisabledPseudoState());
            }

            // Disconnect the ports of the 2 nodes.
            GraphModel.Disconnect(edge);
            graphView.RebuildUI(GraphModel, Store);
            nodeList = graphView.nodes.ToList();

            // Once more, nothing is connected. The collapse button should be enabled.
            Assert.AreEqual(2, nodeList.Count);
            foreach (Node node in nodeList)
            {
                VisualElement collapseButton = node.Q<VisualElement>(name: "collapse-button");
                Assert.False(collapseButton.GetDisabledPseudoState());
            }
        }

        [UnityTest]
        public IEnumerator SelectedNodeCanBeDeleted()
        {
            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            int initialCount = graphView.nodes.ToList().Count;
            Assert.Greater(initialCount, 0);

            Node node = graphView.nodes.First();
            graphView.AddToSelection(node);
            graphView.DeleteSelection();
            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            Assert.AreEqual(initialCount - 1, graphView.nodes.ToList().Count);
        }

        [UnityTest]
        public IEnumerator SelectedEdgeCanBeDeleted()
        {
            var edge = GraphModel.CreateEdge(m_Node1.OutputPorts.First(), m_Node2.InputPorts.First());
            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            int initialCount = window.GraphView.edges.ToList().Count;
            Assert.Greater(initialCount, 0);

            window.GraphView.AddToSelection(edge.GetUI<Edge>(graphView));
            window.GraphView.DeleteSelection();
            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            Assert.AreEqual(initialCount - 1, window.GraphView.edges.ToList().Count);
        }

        [UnityTest]
        public IEnumerator EdgeColorsMatchCustomPortColors()
        {
            graphView.AddToClassList("EdgeColorsMatchCustomPortColors");

            var edge = GraphModel.CreateEdge(m_Node2.InputPorts.First(), m_Node1.OutputPorts.First());
            graphView.RebuildUI(GraphModel, Store);
            // Resolve custom styles.
            yield return null;

            var outputPort = m_Node1.OutputPorts.First().GetUI<Port>(graphView);
            var inputPort = m_Node2.InputPorts.First().GetUI<Port>(graphView);
            var edgeControl = edge.GetUI<Edge>(graphView)?.EdgeControl;

            Assert.IsNotNull(outputPort);
            Assert.IsNotNull(inputPort);
            Assert.IsNotNull(edgeControl);

            Assert.AreEqual(Color.red, inputPort.PortColor);
            Assert.AreEqual(Color.blue, outputPort.PortColor);

            Assert.AreEqual(inputPort.PortColor, edgeControl.InputColor);
            Assert.AreEqual(outputPort.PortColor, edgeControl.OutputColor);
        }
    }
}
