using System.Collections;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine.UIElements;
using UnityEngine.TestTools;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements.Utilities;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    public class GraphElementManipulatorTests : GraphViewTester
    {
        BasicNodeModel m_NodeModel1;
        BasicNodeModel m_NodeModel2;

        class NonDeletableNodeModel : BasicNodeModel
        {
            public override bool IsDeletable => false;
        }

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            m_NodeModel1 = CreateNode("Node 1", new Vector2(0, 0));
            m_NodeModel2 = CreateNode<NonDeletableNodeModel>("Node 2", new Vector2(200, 0));
        }

        [UnityTest]
        public IEnumerator DeletableElementCanBeDeleted()
        {
            graphView.RebuildUI(GraphModel, Store);
            yield return null;
            var node1 = m_NodeModel1.GetUI<Node>(graphView);
            var node2 = m_NodeModel2.GetUI<Node>(graphView);

            // We need to get the graphView in focus for the commands to be properly sent.
            graphView.Focus();

            Assert.AreEqual(2, graphView.graphElements.ToList().Count);

            const bool noAdditiveSelect = false;
            node1.Select(graphView, noAdditiveSelect);

            helpers.ExecuteCommand("Delete");
            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            Assert.AreEqual(1, graphView.graphElements.ToList().Count);

            // Node 2 is not deletable.
            // Selecting it and sending the Delete command should leave the node count unchanged.
            node2.Select(graphView, noAdditiveSelect);

            helpers.ExecuteCommand("Delete");
            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            Assert.AreEqual(1, graphView.graphElements.ToList().Count);
            yield return null;
        }

        void MoveMouseTo(Vector2 start, Vector2 end)
        {
            Vector2 increment = (end - start) / 10;
            for (int i = 0; i < 10; i++)
                helpers.MouseMoveEvent(start + i * increment, start + (i + 1) * increment);
        }

        [Test]
        public void UnparentingElementDuringSelectionDragDoesntThrow()
        {
            graphView.RebuildUI(GraphModel, Store);
            var node1 = m_NodeModel1.GetUI<Node>(graphView);

            graphView.Focus();

            const bool noAdditiveSelect = false;
            node1.Select(graphView, noAdditiveSelect);

            helpers.MouseDownEvent(node1);

            var testMousePosition = new Vector2(10, 0);
            helpers.MouseMoveEvent(Vector2.zero, testMousePosition);

            node1.RemoveFromHierarchy();

            var testMouseEndPosition = new Vector2(15, 30);
            Assert.DoesNotThrow(() => MoveMouseTo(testMousePosition, testMouseEndPosition),
                "Did not expect any errors when moving mouse with selection containing unparented elements");

            // Reset pan and other side effects of the selection drag
            helpers.MouseUpEvent(node1);
        }

        [UnityTest]
        public IEnumerator ZoomWorks()
        {
            VisualElement vc = window.GraphView.contentViewContainer;
            Matrix4x4 transform = vc.transform.matrix;

            Assert.AreEqual(Matrix4x4.identity, vc.transform.matrix);

            var testMousePosition = new Vector2(15, 30);
            int delta = 10;

            Vector2 localMousePosition = vc.WorldToLocal(testMousePosition);
            Vector2 zoomCenter = localMousePosition;
            float x = zoomCenter.x + vc.layout.x;
            float y = zoomCenter.y + vc.layout.y;

            transform *= Matrix4x4.Translate(new Vector3(x, y, 0));
            Vector3 s = Vector3.one / (1 + ContentZoomer.DefaultScaleStep);
            s.z = 1;
            transform *= Matrix4x4.TRS(Vector3.zero, Quaternion.identity, s);
            transform *= Matrix4x4.Translate(new Vector3(-x, -y, 0));

            // The zoomer does pixel alignment to make sure that text stays sharp.
            // We do the same alignment on the translation here.
            transform.m03 = GraphViewStaticBridge.RoundToPixelGrid(transform.m03);
            transform.m13 = GraphViewStaticBridge.RoundToPixelGrid(transform.m13);

            window.SendEvent(new Event
            {
                type = EventType.ScrollWheel,
                mousePosition = testMousePosition,
                delta = new Vector2(delta, delta)
            });
            yield return null;

            //Can't use AreEquals because we need the kEpsilon from ==
            Assert.IsTrue(transform == vc.transform.matrix);
            yield return null;
        }
    }
}
