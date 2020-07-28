using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements.Utilities;
using UnityEngine;
using UnityEngine.UIElements;
using Node = UnityEditor.GraphToolsFoundation.Overdrive.GraphElements.Node;

namespace GraphElements
{
    public class AutoPlacementTestHelper : GraphViewTester
    {
        protected BasicNodeModel FirstNodeModel { get; set; }
        protected BasicNodeModel SecondNodeModel { get; set; }
        protected BasicNodeModel ThirdNodeModel { get; set; }
        protected BasicNodeModel FourthNodeModel { get; set; }
        protected BasicPlacematModel PlacematModel { get; private set; }
        protected BasicStickyNoteModel StickyNoteModel { get; private set; }

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
            FirstNodeModel = CreateNode("Node1", firstNodePos);
            SecondNodeModel = CreateNode("Node2", secondNodePos);
            ThirdNodeModel = CreateNode("Node3", thirdNodePos);
            FourthNodeModel = CreateNode("Node4", fourthNodePos);

            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            Orientation orientation = isVerticalPort ? Orientation.Vertical : Orientation.Horizontal;

            BasicPortModel outputPortFirstNode = FirstNodeModel.AddPort(orientation, Direction.Output, PortCapacity.Single, typeof(float));
            BasicPortModel outputPortSecondNode = SecondNodeModel.AddPort(orientation, Direction.Output, PortCapacity.Single, typeof(float));
            Assert.IsNotNull(outputPortFirstNode);
            Assert.IsNotNull(outputPortSecondNode);

            BasicPortModel intputPortThirdNode = ThirdNodeModel.AddPort(orientation, Direction.Input, PortCapacity.Multi, typeof(float));
            BasicPortModel outputPortThirdNode = ThirdNodeModel.AddPort(orientation, Direction.Output, PortCapacity.Single, typeof(float));
            Assert.IsNotNull(intputPortThirdNode);
            Assert.IsNotNull(outputPortThirdNode);

            BasicPortModel inputPortFourthNode = FourthNodeModel.AddPort(orientation, Direction.Input, PortCapacity.Single, typeof(float));
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
