using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements.Utilities;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    public class GraphElementFrameTests : GraphViewTester
    {
        class FooNode : BasicNodeModel
        {
        }

        class BarNode : BasicNodeModel
        {
        }

        [UnityTest]
        public IEnumerator FrameSelectedNodeAndEdge()
        {
            Vector2 firstNodePosition = new Vector2(400, 400);
            Vector2 secondNodePosition = new Vector2(800, 800);

            var firstNodeModel = CreateNode("First Node", firstNodePosition, 0, 2);
            var secondNodeModel = CreateNode("Second Node", secondNodePosition, 2, 0);

            var startPort = firstNodeModel.OutputPorts.First();
            var endPort = secondNodeModel.InputPorts.First();

            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            var actions = ConnectPorts(startPort, endPort);
            while (actions.MoveNext())
            {
                yield return null;
            }

            var edgeModel = startPort.ConnectedEdges.First();
            var edge = edgeModel.GetUI<Edge>(graphView);
            var secondNode = secondNodeModel.GetUI<Node>(graphView);

            graphView.AddToSelection(edge);
            graphView.AddToSelection(secondNode);

            Assert.AreEqual(0.0, graphView.contentViewContainer.transform.position.x);
            Assert.AreEqual(0.0, graphView.contentViewContainer.transform.position.y);

            graphView.FrameSelection();

            Assert.LessOrEqual(graphView.contentViewContainer.transform.position.x, -firstNodePosition.x / 2);
            Assert.LessOrEqual(graphView.contentViewContainer.transform.position.y, -firstNodePosition.y / 2);
        }

        void AssertSingleSelectedElementTypeAndName(Type modelType, string name)
        {
            Assert.That(graphView.selection.Count, NUnit.Framework.Is.EqualTo(1));
            Assert.That(graphView.selection[0], NUnit.Framework.Is.AssignableTo(typeof(Node)));
            Assert.That((graphView.selection[0] as Node)?.Model, NUnit.Framework.Is.AssignableTo(modelType));
            Assert.That(((graphView.selection[0] as Node)?.Model as IHasTitle)?.Title, NUnit.Framework.Is.EqualTo(name));
        }

        [Test]
        public void FrameNextPrevTest()
        {
            CreateNode<FooNode>("N0", Vector2.zero);
            CreateNode<FooNode>("N1", Vector2.zero);
            CreateNode<FooNode>("N2", Vector2.zero);
            CreateNode<FooNode>("N3", Vector2.zero);

            graphView.RebuildUI(GraphModel, Store);

            graphView.ClearSelection();
            graphView.AddToSelection(graphView.graphElements.First());

            graphView.FrameNext();
            AssertSingleSelectedElementTypeAndName(typeof(FooNode), "N1");

            graphView.FrameNext();
            AssertSingleSelectedElementTypeAndName(typeof(FooNode), "N2");

            graphView.FrameNext();
            AssertSingleSelectedElementTypeAndName(typeof(FooNode), "N3");

            graphView.FrameNext();
            AssertSingleSelectedElementTypeAndName(typeof(FooNode), "N0");

            graphView.FramePrev();
            AssertSingleSelectedElementTypeAndName(typeof(FooNode), "N3");

            graphView.FramePrev();
            AssertSingleSelectedElementTypeAndName(typeof(FooNode), "N2");

            graphView.FramePrev();
            AssertSingleSelectedElementTypeAndName(typeof(FooNode), "N1");

            graphView.FramePrev();
            AssertSingleSelectedElementTypeAndName(typeof(FooNode), "N0");
        }

        [Test]
        public void FrameNextPrevWithoutSelectionTest()
        {
            CreateNode<FooNode>("N0", Vector2.zero);
            CreateNode<FooNode>("N1", Vector2.zero);
            CreateNode<FooNode>("N2", Vector2.zero);
            CreateNode<FooNode>("N3", Vector2.zero);

            graphView.RebuildUI(GraphModel, Store);

            // Reset selection for next test
            graphView.ClearSelection();

            graphView.FrameNext();
            AssertSingleSelectedElementTypeAndName(typeof(FooNode), "N0");

            // Reset selection for prev test
            graphView.ClearSelection();

            graphView.FramePrev();
            AssertSingleSelectedElementTypeAndName(typeof(FooNode), "N3");
        }

        [Test]
        public void FrameNextPrevPredicateTest()
        {
            CreateNode<FooNode>("F0", Vector2.zero);
            CreateNode<FooNode>("F1", Vector2.zero);
            CreateNode<BarNode>("B0", Vector2.zero);
            CreateNode<BarNode>("B1", Vector2.zero);
            CreateNode<FooNode>("F2", Vector2.zero);
            CreateNode<BarNode>("B2", Vector2.zero);

            graphView.RebuildUI(GraphModel, Store);

            graphView.ClearSelection();
            graphView.AddToSelection(graphView.graphElements.First());

            graphView.FrameNext(x => x.Model is FooNode);
            AssertSingleSelectedElementTypeAndName(typeof(FooNode), "F1");

            graphView.FrameNext(IsFooNode);
            AssertSingleSelectedElementTypeAndName(typeof(FooNode), "F2");

            graphView.FrameNext(x => x.Model is FooNode);
            AssertSingleSelectedElementTypeAndName(typeof(FooNode), "F0");

            graphView.FramePrev(IsFooNode);
            AssertSingleSelectedElementTypeAndName(typeof(FooNode), "F2");

            graphView.ClearSelection();
            graphView.AddToSelection(graphView.graphElements.First());

            graphView.FrameNext(x => (x.Model as IHasTitle)?.Title.Contains("0") ?? false);
            AssertSingleSelectedElementTypeAndName(typeof(BasicNodeModel), "B0");

            graphView.FrameNext(x => (x.Model as IHasTitle)?.Title.Contains("0") ?? false);
            AssertSingleSelectedElementTypeAndName(typeof(BasicNodeModel), "F0");
        }

        private bool IsFooNode(GraphElement element)
        {
            return element.Model is FooNode;
        }
    }
}
