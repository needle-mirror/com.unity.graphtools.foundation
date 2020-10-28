using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class GraphElementSelectTests : GraphViewTester
    {
        IONodeModel m_Node1;
        IONodeModel m_Node2;
        IONodeModel m_Node3;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            m_Node1 = CreateNode("Node 1", new Vector2(10, 30));
            m_Node2 = CreateNode("Node 2", new Vector2(270, 30));
            m_Node3 = CreateNode("Node 3", new Vector2(400, 30)); // overlaps m_Node2
        }

        void GetUI(out Node node1, out Node node2, out Node node3)
        {
            node1 = m_Node1.GetUI<Node>(graphView);
            node2 = m_Node2.GetUI<Node>(graphView);
            node3 = m_Node3.GetUI<Node>(graphView);
        }

        Rect RectAroundNodes(Node node1, Node node2, Node node3)
        {
            // Generate a rectangle to select all the elements
            Rect rectangle = RectUtils.Encompass(RectUtils.Encompass(node1.worldBound, node2.worldBound), node3.worldBound);
            rectangle = RectUtils.Inflate(rectangle, 1, 1, 1, 1);
            return rectangle;
        }

        [UnityTest]
        public IEnumerator ElementCanBeSelected()
        {
            graphView.RebuildUI(GraphModel, Store);
            yield return null;
            GetUI(out var node1, out var node2, out var node3);

            helpers.Click(node1);

            yield return null;

            Assert.True(node1?.Selected);
            Assert.False(node2?.Selected);
            Assert.False(node3?.Selected);
        }

        [UnityTest]
        public IEnumerator SelectingNewElementUnselectsPreviousOne()
        {
            graphView.RebuildUI(GraphModel, Store);
            yield return null;
            GetUI(out var node1, out var node2, out var node3);

            // Select elem 1. All other elems should be unselected.
            helpers.Click(node1);

            yield return null;

            Assert.True(node1.Selected);
            Assert.False(node2.Selected);
            Assert.False(node3.Selected);

            // Select elem 2. All other elems should be unselected.
            helpers.Click(node2);

            yield return null;

            Assert.False(node1.Selected);
            Assert.True(node2.Selected);
            Assert.False(node3.Selected);
        }

        [UnityTest]
        public IEnumerator SelectionSurvivesNodeRemoval()
        {
            const string key = "node42";
            const string wrongKey = "node43";

            // Create the node.
            var nodeModel = CreateNode(key, new Vector2(200, 200));
            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            var node = nodeModel.GetUI<Node>(graphView);
            Assert.IsNotNull(node);
            node.viewDataKey = key;

            // Add to selection.
            graphView.AddToSelection(node);
            Assert.True(node.Selected);

            // Remove node.
            graphView.RemoveElement(node);
            Assert.False(node.Selected);

            // Add node back and restore selection.
            graphView.AddElement(node);
            Assert.True(node.Selected);

            // Remove and add back but with a different viewDataKey.
            graphView.RemoveElement(node);
            node.viewDataKey = wrongKey;
            graphView.AddElement(node);
            Assert.False(node.Selected);
        }

        EventModifiers modifiers
        {
            get
            {
                return Application.platform == RuntimePlatform.OSXEditor ? EventModifiers.Command : EventModifiers.Control;
            }
        }

        [UnityTest]
        public IEnumerator SelectingNewElementWithActionAddsToSelection()
        {
            graphView.RebuildUI(GraphModel, Store);
            yield return null;
            GetUI(out var node1, out var node2, out var node3);

            // Select elem 1. All other elems should be unselected.
            helpers.Click(node1);

            yield return null;

            // Select elem 2 with control. 1 and 2 should be selected
            helpers.Click(node2, eventModifiers: modifiers);

            yield return null;

            Assert.True(node1.Selected);
            Assert.True(node2.Selected);
            Assert.False(node3.Selected);
        }

        [UnityTest]
        public IEnumerator SelectingSelectedElementWithActionModifierRemovesFromSelection()
        {
            graphView.RebuildUI(GraphModel, Store);
            yield return null;
            GetUI(out var node1, out var node2, out var node3);

            // Select elem 1. All other elems should be unselected.
            helpers.Click(node1);

            yield return null;

            // Select elem 2 with control. 1 and 2 should be selected
            helpers.Click(node2, eventModifiers: modifiers);

            yield return null;

            // Select elem 1 with control. Only 2 should be selected
            helpers.Click(node1, eventModifiers: modifiers);

            yield return null;

            Assert.False(node1.Selected);
            Assert.True(node2.Selected);
            Assert.False(node3.Selected);
        }

        // Taken from internal QuadTree utility
        static bool Intersection(Rect r1, Rect r2, out Rect intersection)
        {
            if (!r1.Overlaps(r2) && !r2.Overlaps(r1))
            {
                intersection = new Rect(0, 0, 0, 0);
                return false;
            }

            float left = Mathf.Max(r1.xMin, r2.xMin);
            float top = Mathf.Max(r1.yMin, r2.yMin);

            float right = Mathf.Min(r1.xMax, r2.xMax);
            float bottom = Mathf.Min(r1.yMax, r2.yMax);
            intersection = new Rect(left, top, right - left, bottom - top);
            return true;
        }

        [UnityTest]
        public IEnumerator ClickOnTwoOverlappingElementsSelectsTopOne()
        {
            graphView.RebuildUI(GraphModel, Store);
            yield return null;
            GetUI(out var node1, out var node2, out var node3);

            // Find the intersection between those two nodes and click right in the middle
            Rect intersection;
            Assert.IsTrue(Intersection(node2.worldBound, node3.worldBound, out intersection), "Expected rectangles to intersect");

            helpers.Click(intersection.center);

            yield return null;

            Assert.False(node1.Selected);
            Assert.False(node2.Selected);
            Assert.True(node3.Selected);
        }

        [UnityTest]
        public IEnumerator RectangleSelectionWorks()
        {
            graphView.RebuildUI(GraphModel, Store);
            yield return null;
            GetUI(out var node1, out var node2, out var node3);

            Rect rectangle = RectAroundNodes(node1, node2, node3);

            helpers.DragTo(rectangle.max, rectangle.min);

            yield return null;

            Assert.True(node1.Selected);
            Assert.True(node2.Selected);
            Assert.True(node3.Selected);
        }

        [UnityTest]
        public IEnumerator RectangleSelectionWithActionKeyWorks()
        {
            graphView.RebuildUI(GraphModel, Store);
            yield return null;
            GetUI(out var node1, out var node2, out var node3);

            graphView.AddToSelection(node1);
            Assert.True(node1.Selected);
            Assert.False(node2.Selected);
            Assert.False(node3.Selected);

            Rect rectangle = RectAroundNodes(node1, node2, node3);

            // Reselect all.
            helpers.DragTo(rectangle.min, rectangle.max, eventModifiers: modifiers);

            yield return null;

            Assert.False(node1.Selected);
            Assert.True(node2.Selected);
            Assert.True(node3.Selected);
        }

        [UnityTest]
        public IEnumerator FreehandSelectionWorks()
        {
            graphView.RebuildUI(GraphModel, Store);
            yield return null;
            GetUI(out var node1, out var node2, out var node3);

            Rect rectangle = RectAroundNodes(node1, node2, node3);

            float lineAcrossNodes = rectangle.y + (rectangle.yMax - rectangle.y) * 0.5f;
            Vector2 startPoint = new Vector2(rectangle.xMax, lineAcrossNodes);
            Vector2 endPoint = new Vector2(rectangle.xMin, lineAcrossNodes);
            helpers.DragTo(startPoint, endPoint, eventModifiers: EventModifiers.Shift, steps: 10);

            yield return null;

            Assert.True(node1.Selected);
            Assert.True(node2.Selected);
            Assert.True(node3.Selected);
        }

        [UnityTest]
        public IEnumerator FreehandDeleteWorks()
        {
            graphView.RebuildUI(GraphModel, Store);
            yield return null;
            GetUI(out var node1, out var node2, out var node3);

            Rect rectangle = RectAroundNodes(node1, node2, node3);

            float lineAcrossNodes = rectangle.y + (rectangle.yMax - rectangle.y) * 0.5f;
            Vector2 startPoint = new Vector2(rectangle.xMax, lineAcrossNodes);
            Vector2 endPoint = new Vector2(rectangle.xMin, lineAcrossNodes);
            helpers.DragTo(startPoint, endPoint, eventModifiers: EventModifiers.Shift | EventModifiers.Alt, steps: 10);

            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            // After manipulation we should have only zero elements left.
            Assert.AreEqual(0, graphView.GraphElements.ToList().Count);
        }

        [Test]
        public void AddingElementToSelectionTwiceDoesNotAddTheSecondTime()
        {
            graphView.RebuildUI(GraphModel, Store);
            GetUI(out var node1, out var node2, out _);

            Assert.AreEqual(0, graphView.Selection.Count);

            graphView.AddToSelection(node1);
            Assert.AreEqual(1, graphView.Selection.Count);

            // Add same element again, should have no impact on selection
            graphView.AddToSelection(node1);
            Assert.AreEqual(1, graphView.Selection.Count);

            // Add other element
            graphView.AddToSelection(node2);
            Assert.AreEqual(2, graphView.Selection.Count);
        }

        [Test]
        public void RemovingElementFromSelectionTwiceDoesThrowException()
        {
            graphView.RebuildUI(GraphModel, Store);
            GetUI(out var node1, out var node2, out _);

            graphView.AddToSelection(node1);
            graphView.AddToSelection(node2);
            Assert.AreEqual(2, graphView.Selection.Count);

            graphView.RemoveFromSelection(node2);
            Assert.AreEqual(1, graphView.Selection.Count);

            // Remove the same item again, should have no impact on selection
            graphView.RemoveFromSelection(node2);
            Assert.AreEqual(1, graphView.Selection.Count);

            // Remove other element
            graphView.RemoveFromSelection(node1);
            Assert.AreEqual(0, graphView.Selection.Count);
        }
    }
}
