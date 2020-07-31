using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements.Utilities;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace GraphElements
{
    public class AutoAlignmentTests : GraphViewTester
    {
        BasicNodeModel FirstNodeModel { get; set; }
        BasicNodeModel SecondNodeModel { get; set; }
        BasicPlacematModel PlacematModel { get; set; }
        BasicStickyNoteModel StickyNoteModel { get; set; }

        Node m_FirstNode;
        Node m_SecondNode;
        Placemat m_Placemat;
        StickyNote m_StickyNote;

        static readonly Vector2 k_SelectionOffset = new Vector2(50, 50);

        IEnumerator SetupElements(bool selectAll, Vector2 firstNodePos, Vector2 secondNodePos, Vector2 placematPos, Vector2 stickyNotePos)
        {
            var actions = CreateElements(firstNodePos, secondNodePos, placematPos, stickyNotePos);
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = SelectElements(selectAll);
            while (actions.MoveNext())
            {
                yield return null;
            }
        }

        IEnumerator CreateElements(Vector2 firstNodePos, Vector2 secondNodePos, Vector2 placematPos, Vector2 stickyNotePos)
        {
            FirstNodeModel = CreateNode("Node1", firstNodePos);
            SecondNodeModel = CreateNode("Node2", secondNodePos);
            PlacematModel = CreatePlacemat(new Rect(placematPos, new Vector2(200, 200)), "Placemat");
            StickyNoteModel = CreateSticky("Sticky", "", new Rect(stickyNotePos, new Vector2(200, 200)));

            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            // Get the UI elements
            m_FirstNode = FirstNodeModel.GetUI<Node>(graphView);
            m_SecondNode = SecondNodeModel.GetUI<Node>(graphView);
            m_Placemat = PlacematModel.GetUI<Placemat>(graphView);
            m_StickyNote = StickyNoteModel.GetUI<StickyNote>(graphView);
            Assert.IsNotNull(m_FirstNode);
            Assert.IsNotNull(m_SecondNode);
            Assert.IsNotNull(m_Placemat);
            Assert.IsNotNull(m_StickyNote);
        }

        IEnumerator SelectElements(bool selectAll)
        {
            Vector2 worldPosNode1 = graphView.contentViewContainer.LocalToWorld(m_FirstNode.layout.position);
            Vector2 worldPosNode2 = graphView.contentViewContainer.LocalToWorld(m_SecondNode.layout.position);
            Vector2 worldPosPlacemat = graphView.contentViewContainer.LocalToWorld(m_Placemat.layout.position);
            Vector2 worldPosStickyNote = graphView.contentViewContainer.LocalToWorld(m_StickyNote.layout.position);

            Vector2 selectionPosNode1 = worldPosNode1 + k_SelectionOffset;
            Vector2 selectionPosNode2 = worldPosNode2 + k_SelectionOffset;
            Vector2 selectionPosPlacemat = worldPosPlacemat + k_SelectionOffset;
            Vector2 selectionPosStickyNote = worldPosStickyNote + k_SelectionOffset;

            // Select Node1
            var actions = SelectElement(selectionPosNode1);
            while (actions.MoveNext())
            {
                yield return null;
            }

            // Move mouse to Node2
            helpers.MouseMoveEvent(selectionPosNode1, selectionPosNode2, MouseButton.LeftMouse, EventModifiers.Control);
            yield return null;

            // Select Node2
            actions = SelectElement(selectionPosNode2);
            helpers.MouseDownEvent(selectionPosNode2, MouseButton.LeftMouse, EventModifiers.Control);
            yield return null;
            while (actions.MoveNext())
            {
                yield return null;
            }

            // Move mouse to Placemat
            helpers.MouseMoveEvent(selectionPosNode2, selectionPosPlacemat, MouseButton.LeftMouse, EventModifiers.Control);
            yield return null;

            // Select Placemat
            actions = SelectElement(selectionPosPlacemat);
            while (actions.MoveNext())
            {
                yield return null;
            }

            // Move mouse to StickyNote
            helpers.MouseMoveEvent(selectionPosPlacemat, selectionPosStickyNote, MouseButton.LeftMouse, EventModifiers.Control);
            yield return null;

            // Select StickyNot
            actions = SelectElement(selectionPosStickyNote);
            while (actions.MoveNext())
            {
                yield return null;
            }

            if (!selectAll)
            {
                // UnSelect StickyNot
                actions = SelectElement(selectionPosStickyNote);
                while (actions.MoveNext())
                {
                    yield return null;
                }
            }
        }

        IEnumerator SelectElement(Vector2 selectedElementPos)
        {
            helpers.MouseDownEvent(selectedElementPos, MouseButton.LeftMouse, EventModifiers.Control);
            yield return null;
            helpers.MouseUpEvent(selectedElementPos, MouseButton.LeftMouse, EventModifiers.Control);
            yield return null;
        }

        IEnumerator AlignElements(AutoAlignmentHelper.AlignmentReference reference)
        {
            AutoAlignmentHelper.SendAlignAction(graphView, reference);
            yield return null;

            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            // Get the UI elements
            m_FirstNode = FirstNodeModel.GetUI<Node>(graphView);
            m_SecondNode = SecondNodeModel.GetUI<Node>(graphView);
            m_Placemat = PlacematModel.GetUI<Placemat>(graphView);
            m_StickyNote = StickyNoteModel.GetUI<StickyNote>(graphView);
            Assert.IsNotNull(m_FirstNode);
            Assert.IsNotNull(m_SecondNode);
            Assert.IsNotNull(m_Placemat);
            Assert.IsNotNull(m_StickyNote);
        }

        [UnityTest]
        public IEnumerator AlignElementsToTop()
        {
            // Config:
            //
            // --+-----+--+-----+--+-----+--+-----+-- top
            //   |Node1|  |Node2|  |place|  |stick|
            //   +-----+  +-----+  +-----+  +-----+

            const float expectedTopValue = 10;
            Vector2 firstNodePos = new Vector2(0, 50);
            Vector2 secondNodePos = new Vector2(200, expectedTopValue);
            Vector2 placematPos = new Vector2(400, 300);
            Vector2 stickyNotePos = new Vector2(600, 200);

            var actions = SetupElements(true, firstNodePos, secondNodePos, placematPos, stickyNotePos);
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = AlignElements(AutoAlignmentHelper.AlignmentReference.Top);
            while (actions.MoveNext())
            {
                yield return null;
            }

            Assert.AreEqual(expectedTopValue, m_FirstNode.layout.yMin);
            Assert.AreEqual(expectedTopValue, m_SecondNode.layout.yMin);
            Assert.AreEqual(expectedTopValue, m_Placemat.layout.yMin);
            Assert.AreEqual(expectedTopValue, m_StickyNote.layout.yMin);
        }

        [UnityTest]
        public IEnumerator AlignElementsToBottom()
        {
            // Config:
            //
            //   +-----+  +-----+  +-----+  +-----+
            //   |Node1|  |Node2|  |place|  |stick|
            // --+-----+--+-----+--+-----+--+-----+-- bottom

            const float expectedBottomValue = 300;
            Vector2 firstNodePos = new Vector2(0, 50);
            Vector2 secondNodePos = new Vector2(200, 100);
            Vector2 placematPos = new Vector2(400, expectedBottomValue);
            Vector2 stickyNotePos = new Vector2(600, 200);

            var actions = SetupElements(true, firstNodePos, secondNodePos, placematPos, stickyNotePos);
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = AlignElements(AutoAlignmentHelper.AlignmentReference.Bottom);
            while (actions.MoveNext())
            {
                yield return null;
            }

            Assert.AreEqual(expectedBottomValue + m_FirstNode.layout.height, m_FirstNode.layout.yMax);
            Assert.AreEqual(expectedBottomValue + m_SecondNode.layout.height, m_SecondNode.layout.yMax);
            Assert.AreEqual(expectedBottomValue + m_Placemat.layout.height, m_Placemat.layout.yMax);
            Assert.AreEqual(expectedBottomValue + m_StickyNote.layout.height, m_StickyNote.layout.yMax);
        }

        [UnityTest]
        public IEnumerator AlignElementsToLeft()
        {
            // Config:
            //    |
            //    |-----+
            //    |Node1|
            //    |-----+
            //    |-----+
            //    |Node2|
            //    |-----+
            //    |-----+
            //    |stick|
            //    |-----+
            //    |-----+
            //    |place|
            //    |-----+
            //left|

            const float expectedLeftValue = 0;
            Vector2 firstNodePos = new Vector2(expectedLeftValue, 50);
            Vector2 secondNodePos = new Vector2(200, 100);
            Vector2 placematPos = new Vector2(400, 300);
            Vector2 stickyNotePos = new Vector2(600, 200);

            var actions = SetupElements(true, firstNodePos, secondNodePos, placematPos, stickyNotePos);
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = AlignElements(AutoAlignmentHelper.AlignmentReference.Left);
            while (actions.MoveNext())
            {
                yield return null;
            }

            Assert.AreEqual(expectedLeftValue, m_FirstNode.layout.xMin);
            Assert.AreEqual(expectedLeftValue, m_SecondNode.layout.xMin);
            Assert.AreEqual(expectedLeftValue, m_Placemat.layout.xMin);
            Assert.AreEqual(expectedLeftValue, m_StickyNote.layout.xMin);
        }

        [UnityTest]
        public IEnumerator AlignElementsToRight()
        {
            // Config:
            //         |
            //   +-----|
            //   |Node1|
            //   +-----|
            //   +-----|
            //   |Node2|
            //   +-----|
            //   +-----|
            //   |stick|
            //   +-----|
            //   +-----|
            //   |place|
            //   +-----|
            //         | right

            const float expectedRightValue = 600;
            Vector2 firstNodePos = new Vector2(0, 50);
            Vector2 secondNodePos = new Vector2(200, 100);
            Vector2 placematPos = new Vector2(400, 300);
            Vector2 stickyNotePos = new Vector2(expectedRightValue, 200);

            var actions = SetupElements(true, firstNodePos, secondNodePos, placematPos, stickyNotePos);
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = AlignElements(AutoAlignmentHelper.AlignmentReference.Right);
            while (actions.MoveNext())
            {
                yield return null;
            }

            Assert.AreEqual(expectedRightValue + m_FirstNode.layout.width, m_FirstNode.layout.xMax);
            Assert.AreEqual(expectedRightValue + m_SecondNode.layout.width, m_SecondNode.layout.xMax);
            Assert.AreEqual(expectedRightValue + m_Placemat.layout.width, m_Placemat.layout.xMax);
            Assert.AreEqual(expectedRightValue + m_StickyNote.layout.width, m_StickyNote.layout.xMax);
        }

        [UnityTest]
        public IEnumerator AlignElementsToHorizontalCenter()
        {
            // Config:
            //      |
            //   +--|--+
            //   |Node2|
            //   +--|--+
            //   +--|--+
            //   |stick|
            //   +--|--+
            // +----|----+
            // | placemat|
            // +----|----+
            //   +--|--+
            //   |Node1|
            //   +--|--+
            //      | horizontal center

            Vector2 firstNodePos = new Vector2(0, 400);
            Vector2 secondNodePos = new Vector2(200, 100);
            Vector2 placematPos = new Vector2(400, 300);
            Vector2 stickyNotePos = new Vector2(600, 200);

            var actions = SetupElements(true, firstNodePos, secondNodePos, placematPos, stickyNotePos);
            while (actions.MoveNext())
            {
                yield return null;
            }

            float expectedHorizontalCenterValue = (m_FirstNode.layout.center.x + m_SecondNode.layout.center.x + m_Placemat.layout.center.x + m_StickyNote.layout.center.x) / 4;

            actions = AlignElements(AutoAlignmentHelper.AlignmentReference.HorizontalCenter);
            while (actions.MoveNext())
            {
                yield return null;
            }

            Assert.AreEqual(expectedHorizontalCenterValue, m_FirstNode.layout.center.x);
            Assert.AreEqual(expectedHorizontalCenterValue, m_SecondNode.layout.center.x);
            Assert.AreEqual(expectedHorizontalCenterValue, m_Placemat.layout.center.x);
            Assert.AreEqual(expectedHorizontalCenterValue, m_StickyNote.layout.center.x);
        }

        [UnityTest]
        public IEnumerator AlignElementsToVerticalCenter()
        {
            // Config:
            //
            //   +-----+  +-----+  +-----+  +-----+
            //  --Node2----stick----place----Node1-- vertical center
            //   +-----+  +-----+  +-----+  +-----+

            Vector2 firstNodePos = new Vector2(0, 400);
            Vector2 secondNodePos = new Vector2(200, 100);
            Vector2 placematPos = new Vector2(400, 300);
            Vector2 stickyNotePos = new Vector2(600, 200);

            var actions = SetupElements(true, firstNodePos, secondNodePos, placematPos, stickyNotePos);
            while (actions.MoveNext())
            {
                yield return null;
            }

            float expectedVerticalCenterValue = (m_FirstNode.layout.center.y + m_SecondNode.layout.center.y + m_Placemat.layout.center.y + m_StickyNote.layout.center.y) / 4;

            actions = AlignElements(AutoAlignmentHelper.AlignmentReference.VerticalCenter);
            while (actions.MoveNext())
            {
                yield return null;
            }

            Assert.AreEqual(expectedVerticalCenterValue, m_FirstNode.layout.center.y);
            Assert.AreEqual(expectedVerticalCenterValue, m_SecondNode.layout.center.y);
            Assert.AreEqual(expectedVerticalCenterValue, m_Placemat.layout.center.y);
            Assert.AreEqual(expectedVerticalCenterValue, m_StickyNote.layout.center.y);
        }

        [UnityTest]
        public IEnumerator AlignOnlySelectedElements()
        {
            // Config:
            //
            // +-----+--+-----+--+-----+------- top
            // |Node1|  |Node2|  |place|
            // +-----+  +-----+  +-----+
            //
            //                          +-----+
            //                          |stick| <- not selected
            //                          +-----+
            //

            const float expectedTopValue = 0;

            Vector2 firstNodePos = new Vector2(0, expectedTopValue);
            Vector2 secondNodePos = new Vector2(200, 200);
            Vector2 placematPos = new Vector2(400, 400);
            Vector2 stickyNotePos = new Vector2(600, 400);

            var actions = SetupElements(false, firstNodePos, secondNodePos, placematPos, stickyNotePos);
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = AlignElements(AutoAlignmentHelper.AlignmentReference.Top);
            while (actions.MoveNext())
            {
                yield return null;
            }

            Assert.AreEqual(expectedTopValue, m_FirstNode.layout.yMin);
            Assert.AreEqual(expectedTopValue, m_SecondNode.layout.yMin);
            Assert.AreEqual(expectedTopValue, m_Placemat.layout.yMin);
            Assert.AreNotEqual(expectedTopValue, m_StickyNote.layout.yMin);
        }

        [UnityTest]
        public IEnumerator AlignPlacemat()
        {
            // Config:
            //   +-----+
            // +-|Node1|-+
            // | +-----+ | +-----+  +------+
            // | placemat| |Node2|  |sticky|
            // +---------+-+-----+--+------+--- bottom

            const float expectedBottomValue = 300;

            Vector2 placematPos = new Vector2(0, 150);
            Vector2 firstNodePos = new Vector2(0, 0); // First node is on the placemat
            Vector2 secondNodePos = new Vector2(200, 200);
            Vector2 stickyNotePos = new Vector2(600, expectedBottomValue);

            var actions = SetupElements(true, firstNodePos, secondNodePos, placematPos, stickyNotePos);
            while (actions.MoveNext())
            {
                yield return null;
            }


            actions = AlignElements(AutoAlignmentHelper.AlignmentReference.Bottom);
            while (actions.MoveNext())
            {
                yield return null;
            }

            // First node follow placemat movement, but does not align to the bottom
            Assert.AreNotEqual(expectedBottomValue + m_FirstNode.layout.height, m_FirstNode.layout.yMax);
            Assert.AreEqual(expectedBottomValue + m_Placemat.layout.height, m_Placemat.layout.yMax);

            Assert.AreEqual(expectedBottomValue + m_SecondNode.layout.height, m_SecondNode.layout.yMax);
            Assert.AreEqual(expectedBottomValue + m_StickyNote.layout.height, m_StickyNote.layout.yMax);
        }

        [UnityTest]
        public IEnumerator AlignElementOnPlacemat()
        {
            // Config:
            // +---------+
            // | placemat|
            // |         |
            // | +-----+ | +-----+  +------+
            // +-|Node1|-+ |Node2|  |sticky|
            // --+-----+---+-----+--+------+--- bottom

            const float expectedBottomValue = 300;

            Vector2 placematPos = new Vector2(0, 0);
            Vector2 firstNodePos = new Vector2(0, 150); // First node is on the placemat
            Vector2 secondNodePos = new Vector2(200, 200);
            Vector2 stickyNotePos = new Vector2(600, expectedBottomValue);

            var actions = SetupElements(true, firstNodePos, secondNodePos, placematPos, stickyNotePos);
            while (actions.MoveNext())
            {
                yield return null;
            }


            actions = AlignElements(AutoAlignmentHelper.AlignmentReference.Bottom);
            while (actions.MoveNext())
            {
                yield return null;
            }

            // First node's yMax is greater than placemat's yMax: first node's yMax aligns to bottom, but not the placemat's
            Assert.AreEqual(expectedBottomValue + m_FirstNode.layout.height, m_FirstNode.layout.yMax);
            Assert.AreNotEqual(expectedBottomValue + m_Placemat.layout.height, m_Placemat.layout.yMax);

            Assert.AreEqual(expectedBottomValue + m_SecondNode.layout.height, m_SecondNode.layout.yMax);
            Assert.AreEqual(expectedBottomValue + m_StickyNote.layout.height, m_StickyNote.layout.yMax);
        }
    }
}
