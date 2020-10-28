using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class AutoPlacementTestHelper : GraphViewTester
    {
        protected IInOutPortsNode FirstNodeModel { get; set; }
        protected IInOutPortsNode SecondNodeModel { get; set; }
        protected IInOutPortsNode ThirdNodeModel { get; set; }
        protected IInOutPortsNode FourthNodeModel { get; set; }
        protected IPlacematModel PlacematModel { get; private set; }
        protected IStickyNoteModel StickyNoteModel { get; private set; }

        protected Node m_FirstNode;
        protected Node m_SecondNode;
        protected Node m_ThirdNode;
        protected Node m_FourthNode;
        protected Placemat m_Placemat;
        protected StickyNote m_StickyNote;

        protected static readonly Vector2 k_SelectionOffset = new Vector2(50, 50);

        protected IEnumerator SetupElements(bool smallerSize, Vector2 firstNodePos, Vector2 secondNodePos, Vector2 placematPos, Vector2 stickyNotePos)
        {
            var actions = CreateElements(firstNodePos, secondNodePos, placematPos, stickyNotePos, smallerSize);
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = SelectElements();
            while (actions.MoveNext())
            {
                yield return null;
            }
        }

        protected IEnumerator CreateConnectedNodes(Vector2 firstNodePos, Vector2 secondNodePos, Vector2 thirdNodePos, Vector2 fourthNodePos, bool isVerticalPort)
        {
            FirstNodeModel = CreateNode("Node1", firstNodePos, 0, 0, 0, 1);
            SecondNodeModel = CreateNode("Node2", secondNodePos, 0, 0, 0, 1);
            ThirdNodeModel = CreateNode("Node3", thirdNodePos, 0, 0, 1, 1);
            FourthNodeModel = CreateNode("Node4", fourthNodePos, 0, 0, 1);

            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            Orientation orientation = isVerticalPort ? Orientation.Vertical : Orientation.Horizontal;

            IPortModel outputPortFirstNode = FirstNodeModel.OutputsByDisplayOrder[0];
            IPortModel outputPortSecondNode = SecondNodeModel.OutputsByDisplayOrder[0];
            Assert.IsNotNull(outputPortFirstNode);
            Assert.IsNotNull(outputPortSecondNode);

            IPortModel intputPortThirdNode = ThirdNodeModel.InputsByDisplayOrder[0];
            IPortModel outputPortThirdNode = ThirdNodeModel.OutputsByDisplayOrder[0];
            Assert.IsNotNull(intputPortThirdNode);
            Assert.IsNotNull(outputPortThirdNode);

            IPortModel inputPortFourthNode = FourthNodeModel.InputsByDisplayOrder[0];
            Assert.IsNotNull(inputPortFourthNode);

            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            // Connect the ports together
            var actions = ConnectPorts(outputPortFirstNode, intputPortThirdNode);
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = ConnectPorts(outputPortSecondNode, intputPortThirdNode);
            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = ConnectPorts(outputPortThirdNode, inputPortFourthNode);
            while (actions.MoveNext())
            {
                yield return null;
            }

            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            // Get the UI nodes
            m_FirstNode = FirstNodeModel.GetUI<Node>(graphView);
            m_SecondNode = SecondNodeModel.GetUI<Node>(graphView);
            m_ThirdNode = ThirdNodeModel.GetUI<Node>(graphView);
            m_FourthNode = FourthNodeModel.GetUI<Node>(graphView);
            Assert.IsNotNull(m_FirstNode);
            Assert.IsNotNull(m_SecondNode);
            Assert.IsNotNull(m_ThirdNode);
            Assert.IsNotNull(FourthNodeModel);
        }

        IEnumerator CreateElements(Vector2 firstNodePos, Vector2 secondNodePos, Vector2 placematPos, Vector2 stickyNotePos, bool smallerSize)
        {
            FirstNodeModel = CreateNode("Node1", firstNodePos);
            SecondNodeModel = CreateNode("Node2", secondNodePos);
            PlacematModel = CreatePlacemat(new Rect(placematPos, new Vector2(200, smallerSize ? 100 : 200)), "Placemat");
            StickyNoteModel = CreateSticky("Sticky", "", new Rect(stickyNotePos, smallerSize ? new Vector2(100, 100) : new Vector2(200, 200)));

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

        protected IEnumerator SelectConnectedNodes()
        {
            Vector2 worldPosNode1 = graphView.contentViewContainer.LocalToWorld(m_FirstNode.layout.position);
            Vector2 worldPosNode2 = graphView.contentViewContainer.LocalToWorld(m_SecondNode.layout.position);
            Vector2 worldPosNode3 = graphView.contentViewContainer.LocalToWorld(m_ThirdNode.layout.position);
            Vector2 worldPosNode4 = graphView.contentViewContainer.LocalToWorld(m_FourthNode.layout.position);

            Vector2 selectionPosNode1 = worldPosNode1 + k_SelectionOffset;
            Vector2 selectionPosNode2 = worldPosNode2 + k_SelectionOffset;
            Vector2 selectionPosNode3 = worldPosNode3 + k_SelectionOffset;
            Vector2 selectionPosNode4 = worldPosNode4 + k_SelectionOffset;

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

            // Move mouse to Node3
            helpers.MouseMoveEvent(selectionPosNode2, selectionPosNode3, MouseButton.LeftMouse, EventModifiers.Control);
            yield return null;

            // Select Node3
            actions = SelectElement(selectionPosNode3);
            while (actions.MoveNext())
            {
                yield return null;
            }

            // Move mouse to Node4
            helpers.MouseMoveEvent(selectionPosNode3, selectionPosNode4, MouseButton.LeftMouse, EventModifiers.Control);
            yield return null;

            // Select Node4
            actions = SelectElement(selectionPosNode4);
            while (actions.MoveNext())
            {
                yield return null;
            }
        }

        IEnumerator SelectElements()
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
        }

        protected IEnumerator SelectElement(Vector2 selectedElementPos)
        {
            helpers.MouseDownEvent(selectedElementPos, MouseButton.LeftMouse, EventModifiers.Control);
            yield return null;
            helpers.MouseUpEvent(selectedElementPos, MouseButton.LeftMouse, EventModifiers.Control);
            yield return null;
        }
    }
}
