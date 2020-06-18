using System;
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
    public class GraphElementMoveTests : GraphViewTester
    {
        static readonly Vector2 k_NodePos = new Vector2(SelectionDragger.k_PanAreaWidth * 2, SelectionDragger.k_PanAreaWidth * 3);
        static readonly Rect k_MinimapRect = new Rect(100, 100, 100, 100);
        Vector2 k_SelectionOffset = new Vector2(100, 100);

        BasicNodeModel m_NodeModel { get; set; }

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            GraphViewSettings.UserSettings.EnableSnapToBorders = false;
            GraphViewSettings.UserSettings.EnableSnapToPort = false;

            m_NodeModel = CreateNode("Movable element", k_NodePos, 0, 1);

            // Add the minimap.
            var miniMap = new MiniMap();
            miniMap.style.left = k_MinimapRect.x;
            miniMap.style.top = k_MinimapRect.y;
            miniMap.style.width = k_MinimapRect.width;
            miniMap.style.height = k_MinimapRect.height;
            graphView.Add(miniMap);
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
            GraphViewStaticBridge.SetTimeSinceStartupCB(null);
        }

        [UnityTest]
        public IEnumerator MovableElementCanBeDragged()
        {
            graphView.RebuildUI(GraphModel, Store);
            yield return null;
            var node = m_NodeModel.GetUI<Node>(graphView);
            Assert.IsNotNull(node);

            Vector2 worldNodePos = graphView.contentViewContainer.LocalToWorld(k_NodePos);
            Vector2 start = worldNodePos + k_SelectionOffset;
            Vector2 moveOffset = new Vector2(10, 10);

            // Move the movable element.
            helpers.MouseDownEvent(start);
            yield return null;

            Vector2 end = start + moveOffset;
            helpers.MouseDragEvent(start, end);
            yield return null;

            helpers.MouseUpEvent(end);
            yield return null;

            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            Assert.AreEqual(k_NodePos.x + moveOffset.x, node.layout.x);
            Assert.AreEqual(k_NodePos.y + moveOffset.y, node.layout.y);

            yield return null;
        }

        [UnityTest]
        public IEnumerator LocallyScaledElementMovesAtSameSpeed()
        {
            graphView.RebuildUI(GraphModel, Store);
            yield return null;
            var node = m_NodeModel.GetUI<Node>(graphView);

            float scaling = 2.0f;
            node.transform.scale = new Vector3(scaling, scaling, scaling);

            yield return MovableElementCanBeDragged();
        }

        [UnityTest]
        public IEnumerator MovableElementCanBeDraggedAndMoveCancelledByEscapeKey()
        {
            graphView.RebuildUI(GraphModel, Store);
            yield return null;
            var node = m_NodeModel.GetUI<Node>(graphView);

            bool needsMouseUp = false;
            bool needsKeyUp = false;

            try
            {
                GraphElement elem = graphView.Q<GraphElement>();

                Vector2 startElementPosition = node.GetPosition().position;

                Vector2 worldNodePos = graphView.contentViewContainer.LocalToWorld(k_NodePos);

                Vector2 start = worldNodePos + k_SelectionOffset;
                Vector2 move = new Vector2(-10, -10);
                // Move the movable element.
                helpers.MouseDownEvent(start);
                needsMouseUp = true;
                yield return null;

                helpers.MouseDragEvent(start, start + move);
                yield return null;

                Vector2 endElementPosition = elem.GetPosition().position;

                Assert.AreEqual(move, (endElementPosition - startElementPosition));

                helpers.KeyDownEvent(KeyCode.Escape);
                needsKeyUp = true;
                yield return null;

                // Back where we started
                //we test both visual and presenter values
                Assert.AreEqual(node.GetPosition().position, startElementPosition);
                Assert.AreEqual(elem.GetPosition().position, startElementPosition);

                helpers.KeyUpEvent(KeyCode.Escape);
                needsKeyUp = false;
                yield return null;

                helpers.MouseUpEvent(start + move);
                needsMouseUp = false;
                yield return null;

                endElementPosition = node.GetPosition().position;
                // We make sure nothing was committed
                Assert.AreEqual(endElementPosition, startElementPosition);
            }
            finally
            {
                if (needsKeyUp)
                    helpers.KeyUpEvent(KeyCode.Escape);
                if (needsMouseUp)
                    helpers.MouseUpEvent(new Vector2(k_NodePos.x - 5, k_NodePos.y + 15));
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator MovableElementCanBeDraggedAtTheBorderToStartPanningInNegativeX()
        {
            graphView.RebuildUI(GraphModel, Store);
            yield return null;
            var node = m_NodeModel.GetUI<Node>(graphView);

            bool needsMouseUp = false;

            Vector2 worldNodePos = graphView.contentViewContainer.LocalToWorld(k_NodePos);
            Vector2 start = worldNodePos + k_SelectionOffset;
            Vector2 panPos = new Vector2(SelectionDragger.k_PanAreaWidth, start.y);

            try
            {
                // Move the movable element.
                helpers.MouseDownEvent(start);
                needsMouseUp = true;
                yield return null;

                long timePassed = 0;
                GraphViewStaticBridge.SetTimeSinceStartupCB(() => timePassed);

                helpers.MouseDragEvent(start, panPos);
                yield return null;

                GraphElement elem = graphView.Q<GraphElement>();

                float deltaX = start.x - panPos.x;
                var newPos = new Vector2(k_NodePos.x - deltaX, k_NodePos.y);

                Assert.AreEqual(newPos, elem.GetPosition().position);

                Assert.AreEqual(0, graphView.viewTransform.position.x);
                Assert.AreEqual(0, graphView.viewTransform.position.y);

                timePassed += SelectionDragger.k_PanInterval;
                graphView.UpdateScheduledEvents();

                float panSpeedAtLocation = SelectionDragger.k_PanSpeed * SelectionDragger.k_MinSpeedFactor;

                yield return null;
                newPos.x -= panSpeedAtLocation;
                Assert.AreEqual(newPos, elem.GetPosition().position);

                Assert.AreEqual(panSpeedAtLocation, graphView.viewTransform.position.x);
                Assert.AreEqual(0, graphView.viewTransform.position.y);

                timePassed += SelectionDragger.k_PanInterval;
                graphView.UpdateScheduledEvents();

                yield return null;
                newPos.x -= panSpeedAtLocation;
                Assert.AreEqual(newPos, elem.GetPosition().position);

                Assert.AreEqual(panSpeedAtLocation * 2, graphView.viewTransform.position.x);
                Assert.AreEqual(0, graphView.viewTransform.position.y);

                helpers.MouseUpEvent(panPos);
                needsMouseUp = false;
                yield return null;

                Assert.AreEqual(newPos, node.GetPosition().position);

                Assert.AreEqual(panSpeedAtLocation * 2, graphView.contentViewContainer.transform.position.x);
                Assert.AreEqual(0, graphView.contentViewContainer.transform.position.y);
            }
            finally
            {
                if (needsMouseUp)
                    helpers.MouseUpEvent(panPos);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator MovableElementCanBeDraggedAtTheBorderToStartPanningInPositiveX()
        {
            graphView.RebuildUI(GraphModel, Store);
            yield return null;
            var node = m_NodeModel.GetUI<Node>(graphView);

            bool needsMouseUp = false;

            Vector2 worldNodePos = graphView.contentViewContainer.LocalToWorld(k_NodePos);
            Vector2 start = worldNodePos + k_SelectionOffset;
            Vector2 panPos = new Vector2(window.position.width - SelectionDragger.k_PanAreaWidth, start.y);

            try
            {
                // Move the movable element.
                helpers.MouseDownEvent(start);
                needsMouseUp = true;
                yield return null;

                long timePassed = 0;
                GraphViewStaticBridge.SetTimeSinceStartupCB(() => timePassed);

                helpers.MouseDragEvent(start, panPos);
                yield return null;

                GraphElement elem = graphView.Q<GraphElement>();

                float deltaX = start.x - panPos.x;

                var newPos = new Vector2(k_NodePos.x - deltaX, k_NodePos.y);

                Assert.AreEqual(newPos, elem.GetPosition().position);

                Assert.AreEqual(0, graphView.viewTransform.position.x);
                Assert.AreEqual(0, graphView.viewTransform.position.y);

                timePassed += SelectionDragger.k_PanInterval;
                graphView.UpdateScheduledEvents();

                float panSpeedAtLocation = SelectionDragger.k_PanSpeed * SelectionDragger.k_MinSpeedFactor;

                yield return null;
                newPos.x += panSpeedAtLocation;
                Assert.AreEqual(newPos, elem.GetPosition().position);

                Assert.AreEqual(-panSpeedAtLocation, graphView.viewTransform.position.x);
                Assert.AreEqual(0, graphView.viewTransform.position.y);

                timePassed += SelectionDragger.k_PanInterval;
                graphView.UpdateScheduledEvents();

                yield return null;
                newPos.x += panSpeedAtLocation;
                Assert.AreEqual(newPos, elem.GetPosition().position);

                Assert.AreEqual(-panSpeedAtLocation * 2, graphView.viewTransform.position.x);
                Assert.AreEqual(0, graphView.viewTransform.position.y);

                helpers.MouseUpEvent(panPos);
                needsMouseUp = false;
                yield return null;

                Assert.AreEqual(newPos, node.GetPosition().position);

                Assert.AreEqual(-panSpeedAtLocation * 2, graphView.contentViewContainer.transform.position.x);
                Assert.AreEqual(0, graphView.contentViewContainer.transform.position.y);
            }
            finally
            {
                if (needsMouseUp)
                    helpers.MouseUpEvent(panPos);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator MovableElementCanBeDraggedAtTheBorderToStartPanningInNegativeY()
        {
            graphView.RebuildUI(GraphModel, Store);
            yield return null;
            var node = m_NodeModel.GetUI<Node>(graphView);

            bool needsMouseUp = false;

            Vector2 worldNodePos = graphView.contentViewContainer.LocalToWorld(k_NodePos);
            Vector2 start = worldNodePos + k_SelectionOffset;

            Vector2 worldPanPos =
                graphView.contentViewContainer.LocalToWorld(
                    new Vector2(SelectionDragger.k_PanAreaWidth, SelectionDragger.k_PanAreaWidth));
            Vector2 panPos = new Vector2(start.x, worldPanPos.y);

            try
            {
                // Move the movable element.
                helpers.MouseDownEvent(start);
                needsMouseUp = true;
                yield return null;

                long timePassed = 0;
                GraphViewStaticBridge.SetTimeSinceStartupCB(() => timePassed);

                helpers.MouseDragEvent(start, panPos);
                yield return null;

                GraphElement elem = graphView.Q<GraphElement>();

                float deltaY = start.y - panPos.y;
                Vector2 newPos = new Vector2(k_NodePos.x, k_NodePos.y - deltaY);

                Assert.AreEqual(newPos, elem.GetPosition().position);

                Assert.AreEqual(0, graphView.viewTransform.position.x);
                Assert.AreEqual(0, graphView.viewTransform.position.y);

                timePassed += SelectionDragger.k_PanInterval;
                graphView.UpdateScheduledEvents();

                float panSpeedAtLocation = SelectionDragger.k_PanSpeed * SelectionDragger.k_MinSpeedFactor;

                yield return null;
                newPos.y -= panSpeedAtLocation;
                Assert.AreEqual(newPos, elem.GetPosition().position);

                Assert.AreEqual(0, graphView.viewTransform.position.x);
                Assert.AreEqual(panSpeedAtLocation, graphView.viewTransform.position.y);

                timePassed += SelectionDragger.k_PanInterval;
                graphView.UpdateScheduledEvents();

                yield return null;
                newPos.y -= panSpeedAtLocation;
                Assert.AreEqual(newPos, elem.GetPosition().position);

                Assert.AreEqual(0, graphView.viewTransform.position.x);
                Assert.AreEqual(panSpeedAtLocation * 2, graphView.viewTransform.position.y);

                helpers.MouseUpEvent(panPos);
                needsMouseUp = false;
                yield return null;

                Assert.AreEqual(newPos, node.GetPosition().position);

                Assert.AreEqual(0, graphView.contentViewContainer.transform.position.x);
                Assert.AreEqual(panSpeedAtLocation * 2, graphView.contentViewContainer.transform.position.y);
            }
            finally
            {
                if (needsMouseUp)
                    helpers.MouseUpEvent(panPos);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator MovableElementCanBeDraggedAtTheBorderToStartPanningInPositiveY()
        {
            graphView.RebuildUI(GraphModel, Store);
            yield return null;
            var node = m_NodeModel.GetUI<Node>(graphView);

            bool needsMouseUp = false;

            Vector2 worldNodePos = graphView.contentViewContainer.LocalToWorld(k_NodePos);
            Vector2 start = worldNodePos + k_SelectionOffset;
            float worldY = graphView.LocalToWorld(new Vector2(0, graphView.layout.height - SelectionDragger.k_PanAreaWidth)).y;
            Vector2 panPos = new Vector2(start.x, worldY);

            try
            {
                // Move the movable element.
                helpers.MouseDownEvent(start);
                needsMouseUp = true;
                yield return null;

                long timePassed = 0;
                GraphViewStaticBridge.SetTimeSinceStartupCB(() => timePassed);

                helpers.MouseDragEvent(start, panPos);
                yield return null;

                float deltaY = start.y - panPos.y;
                Vector2 newPos = new Vector2(k_NodePos.x, k_NodePos.y - deltaY);

                GraphElement elem = graphView.Q<GraphElement>();

                Assert.AreEqual(newPos, elem.GetPosition().position);

                Assert.AreEqual(0, graphView.viewTransform.position.x);
                Assert.AreEqual(0, graphView.viewTransform.position.y);

                timePassed += SelectionDragger.k_PanInterval;
                graphView.UpdateScheduledEvents();

                float panSpeedAtLocation = SelectionDragger.k_PanSpeed * SelectionDragger.k_MinSpeedFactor;

                yield return null;
                newPos.y += panSpeedAtLocation;
                Assert.AreEqual(newPos, elem.GetPosition().position);

                Assert.AreEqual(0, graphView.viewTransform.position.x);
                Assert.AreEqual(-panSpeedAtLocation, graphView.viewTransform.position.y);

                timePassed += SelectionDragger.k_PanInterval;
                graphView.UpdateScheduledEvents();

                yield return null;
                newPos.y += panSpeedAtLocation;
                Assert.AreEqual(newPos, elem.GetPosition().position);

                Assert.AreEqual(0, graphView.viewTransform.position.x);
                Assert.AreEqual(-panSpeedAtLocation * 2, graphView.viewTransform.position.y);

                helpers.MouseUpEvent(panPos);
                needsMouseUp = false;
                yield return null;

                Assert.AreEqual(newPos, node.GetPosition().position);

                Assert.AreEqual(0, graphView.contentViewContainer.transform.position.x);
                Assert.AreEqual(-panSpeedAtLocation * 2, graphView.contentViewContainer.transform.position.y);
            }
            finally
            {
                if (needsMouseUp)
                    helpers.MouseUpEvent(panPos);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator DraggingEdgeToOrOverBorderStartsPanningInNegativeX()
        {
            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            bool needsMouseUp = false;
            var portCenter = Vector2.zero;

            try
            {
                var port = window.GraphView.Q<Port>();

                portCenter = port.GetGlobalCenter();

                helpers.MouseDownEvent(portCenter);
                needsMouseUp = true;
                yield return null;

                long timePassed = 0;
                GraphViewStaticBridge.SetTimeSinceStartupCB(() => timePassed);

                helpers.MouseDragEvent(portCenter, new Vector2(EdgeDragHelper.k_PanAreaWidth, portCenter.y));
                yield return null;

                Assert.AreEqual(0, graphView.viewTransform.position.x);
                Assert.AreEqual(0, graphView.viewTransform.position.y);

                timePassed += EdgeDragHelper.k_PanInterval;
                graphView.UpdateScheduledEvents();

                float panSpeedAtLocation = EdgeDragHelper.k_PanSpeed * 0.5f;
                float totalDisplacement = panSpeedAtLocation;
                Assert.AreEqual(totalDisplacement, graphView.viewTransform.position.x);
                Assert.AreEqual(0, graphView.viewTransform.position.y);

                timePassed += EdgeDragHelper.k_PanInterval;
                graphView.UpdateScheduledEvents();

                totalDisplacement += panSpeedAtLocation;
                Assert.AreEqual(totalDisplacement, graphView.viewTransform.position.x);
                Assert.AreEqual(0, graphView.viewTransform.position.y);

                // Move outside window
                helpers.MouseDragEvent(new Vector2(EdgeDragHelper.k_PanAreaWidth, portCenter.y),
                    new Vector2(-EdgeDragHelper.k_PanAreaWidth, portCenter.y));
                yield return null;

                Assert.AreEqual(totalDisplacement, graphView.viewTransform.position.x);
                Assert.AreEqual(0, graphView.viewTransform.position.y);

                panSpeedAtLocation = EdgeDragHelper.k_MaxPanSpeed;
                timePassed += EdgeDragHelper.k_PanInterval;
                graphView.UpdateScheduledEvents();

                totalDisplacement += panSpeedAtLocation;
                Assert.AreEqual(totalDisplacement, graphView.viewTransform.position.x);
                Assert.AreEqual(0, graphView.viewTransform.position.y);

                timePassed += EdgeDragHelper.k_PanInterval;
                graphView.UpdateScheduledEvents();

                totalDisplacement += panSpeedAtLocation;
                Assert.AreEqual(totalDisplacement, graphView.viewTransform.position.x);
                Assert.AreEqual(0, graphView.viewTransform.position.y);

                helpers.MouseUpEvent(new Vector2(-EdgeDragHelper.k_PanAreaWidth, portCenter.y));
                needsMouseUp = false;
                yield return null;

                Assert.AreEqual(totalDisplacement, graphView.viewTransform.position.x);
                Assert.AreEqual(0, graphView.viewTransform.position.y);
            }
            finally
            {
                if (needsMouseUp)
                    helpers.MouseUpEvent(new Vector2(-EdgeDragHelper.k_PanAreaWidth, portCenter.y));
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator DraggingEdgeToOrOverBorderStartsPanningInPositiveX()
        {
            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            bool needsMouseUp = false;
            var portCenter = Vector2.zero;

            try
            {
                var port = window.GraphView.Q<Port>();

                portCenter = port.GetGlobalCenter();

                helpers.MouseDownEvent(portCenter);
                needsMouseUp = true;
                yield return null;

                long timePassed = 0;
                GraphViewStaticBridge.SetTimeSinceStartupCB(() => timePassed);

                helpers.MouseDragEvent(portCenter,
                    new Vector2(window.position.width - (EdgeDragHelper.k_PanAreaWidth), portCenter.y));
                yield return null;

                Assert.AreEqual(0, graphView.viewTransform.position.x);
                Assert.AreEqual(0, graphView.viewTransform.position.y);

                timePassed += EdgeDragHelper.k_PanInterval;
                graphView.UpdateScheduledEvents();

                float panSpeedAtLocation = -EdgeDragHelper.k_PanSpeed * 0.5f;
                float totalDisplacement = panSpeedAtLocation;
                Assert.AreEqual(totalDisplacement, graphView.viewTransform.position.x);
                Assert.AreEqual(0, graphView.viewTransform.position.y);

                timePassed += EdgeDragHelper.k_PanInterval;
                graphView.UpdateScheduledEvents();

                totalDisplacement += panSpeedAtLocation;
                Assert.AreEqual(totalDisplacement, graphView.viewTransform.position.x);
                Assert.AreEqual(0, graphView.viewTransform.position.y);

                // Move outside window
                helpers.MouseDragEvent(
                    new Vector2(window.position.width - EdgeDragHelper.k_PanAreaWidth, portCenter.y),
                    new Vector2(window.position.width + EdgeDragHelper.k_PanAreaWidth, portCenter.y));
                yield return null;

                Assert.AreEqual(totalDisplacement, graphView.viewTransform.position.x);
                Assert.AreEqual(0, graphView.viewTransform.position.y);

                panSpeedAtLocation = -EdgeDragHelper.k_MaxPanSpeed;
                timePassed += EdgeDragHelper.k_PanInterval;
                graphView.UpdateScheduledEvents();

                totalDisplacement += panSpeedAtLocation;
                Assert.AreEqual(totalDisplacement, graphView.viewTransform.position.x);
                Assert.AreEqual(0, graphView.viewTransform.position.y);

                timePassed += EdgeDragHelper.k_PanInterval;
                graphView.UpdateScheduledEvents();

                totalDisplacement += panSpeedAtLocation;
                Assert.AreEqual(totalDisplacement, graphView.viewTransform.position.x);
                Assert.AreEqual(0, graphView.viewTransform.position.y);

                helpers.MouseUpEvent(new Vector2(window.position.width + EdgeDragHelper.k_PanAreaWidth, portCenter.y));
                needsMouseUp = false;
                yield return null;

                Assert.AreEqual(totalDisplacement, graphView.viewTransform.position.x);
                Assert.AreEqual(0, graphView.viewTransform.position.y);
            }
            finally
            {
                if (needsMouseUp)
                    helpers.MouseUpEvent(new Vector2(window.position.width + EdgeDragHelper.k_PanAreaWidth, portCenter.y));
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator DraggingEdgeToOrOverBorderStartsPanningInNegativeY()
        {
            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            bool needsMouseUp = false;
            var portCenter = Vector2.zero;

            try
            {
                var port = window.GraphView.Q<Port>();

                portCenter = port.GetGlobalCenter();

                helpers.MouseDownEvent(portCenter);
                needsMouseUp = true;
                yield return null;

                long timePassed = 0;
                GraphViewStaticBridge.SetTimeSinceStartupCB(() => timePassed);

                Vector2 originalWorldNodePos = graphView.contentViewContainer.LocalToWorld(new Vector2(portCenter.x, EdgeDragHelper.k_PanAreaWidth));
                helpers.MouseDragEvent(portCenter, originalWorldNodePos);
                yield return null;

                Assert.AreEqual(0, graphView.viewTransform.position.x);
                Assert.AreEqual(0, graphView.viewTransform.position.y);

                timePassed += EdgeDragHelper.k_PanInterval;
                graphView.UpdateScheduledEvents();

                float panSpeedAtLocation = EdgeDragHelper.k_PanSpeed * 0.5f;
                float totalDisplacement = panSpeedAtLocation;

                Assert.AreEqual(0, graphView.viewTransform.position.x);
                Assert.AreEqual(totalDisplacement, graphView.viewTransform.position.y);

                timePassed += EdgeDragHelper.k_PanInterval;
                graphView.UpdateScheduledEvents();

                totalDisplacement += panSpeedAtLocation;
                Assert.AreEqual(0, graphView.viewTransform.position.x);
                Assert.AreEqual(totalDisplacement, graphView.viewTransform.position.y);

                // Move outside window
                Vector2 newWorldNodePos = new Vector2(portCenter.x, -EdgeDragHelper.k_PanAreaWidth);
                helpers.MouseDragEvent(originalWorldNodePos, newWorldNodePos);
                yield return null;

                Assert.AreEqual(0, graphView.viewTransform.position.x);
                Assert.AreEqual(totalDisplacement, graphView.viewTransform.position.y);

                timePassed += EdgeDragHelper.k_PanInterval;
                graphView.UpdateScheduledEvents();

                panSpeedAtLocation = EdgeDragHelper.k_MaxPanSpeed;
                totalDisplacement += panSpeedAtLocation;
                Assert.AreEqual(0, graphView.viewTransform.position.x);
                Assert.AreEqual(totalDisplacement, graphView.viewTransform.position.y);

                timePassed += EdgeDragHelper.k_PanInterval;
                graphView.UpdateScheduledEvents();

                totalDisplacement += panSpeedAtLocation;
                Assert.AreEqual(0, graphView.viewTransform.position.x);
                Assert.AreEqual(totalDisplacement, graphView.viewTransform.position.y);

                helpers.MouseUpEvent(newWorldNodePos);
                needsMouseUp = false;
                yield return null;

                Assert.AreEqual(0, graphView.viewTransform.position.x);
                Assert.AreEqual(totalDisplacement, graphView.viewTransform.position.y);
            }
            finally
            {
                if (needsMouseUp)
                    helpers.MouseUpEvent(new Vector2(portCenter.x, -EdgeDragHelper.k_PanAreaWidth));
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator DraggingEdgeToOrOverBorderStartsPanningInPositiveY()
        {
            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            bool needsMouseUp = false;
            var portCenter = Vector2.zero;
            var worldY = 0.0f;

            try
            {
                var port = window.GraphView.Q<Port>();

                portCenter = port.GetGlobalCenter();

                helpers.MouseDownEvent(portCenter);
                needsMouseUp = true;
                yield return null;

                long timePassed = 0;
                GraphViewStaticBridge.SetTimeSinceStartupCB(() => timePassed);

                worldY = graphView.LocalToWorld(new Vector2(0, graphView.layout.height - EdgeDragHelper.k_PanAreaWidth)).y;
                helpers.MouseDragEvent(portCenter,
                    new Vector2(portCenter.x, worldY));
                yield return null;

                Assert.AreEqual(0, graphView.viewTransform.position.x);
                Assert.AreEqual(0, graphView.viewTransform.position.y);

                timePassed += EdgeDragHelper.k_PanInterval;
                graphView.UpdateScheduledEvents();

                float panSpeedAtLocation = -EdgeDragHelper.k_PanSpeed * 0.5f;
                float totalDisplacement = panSpeedAtLocation;

                Assert.AreEqual(0, graphView.viewTransform.position.x);
                Assert.AreEqual(totalDisplacement, graphView.viewTransform.position.y);

                timePassed += EdgeDragHelper.k_PanInterval;
                graphView.UpdateScheduledEvents();

                totalDisplacement += panSpeedAtLocation;
                Assert.AreEqual(0, graphView.viewTransform.position.x);
                Assert.AreEqual(totalDisplacement, graphView.viewTransform.position.y);

                // Move outside window
                helpers.MouseDragEvent(
                    new Vector2(portCenter.x, worldY),
                    new Vector2(portCenter.x, worldY + 2 * EdgeDragHelper.k_PanAreaWidth));
                yield return null;

                Assert.AreEqual(0, graphView.viewTransform.position.x);
                Assert.AreEqual(totalDisplacement, graphView.viewTransform.position.y);

                timePassed += EdgeDragHelper.k_PanInterval;
                graphView.UpdateScheduledEvents();

                panSpeedAtLocation = -EdgeDragHelper.k_MaxPanSpeed;
                totalDisplacement += panSpeedAtLocation;
                Assert.AreEqual(0, graphView.viewTransform.position.x);
                Assert.AreEqual(totalDisplacement, graphView.viewTransform.position.y);

                timePassed += EdgeDragHelper.k_PanInterval;
                graphView.UpdateScheduledEvents();

                totalDisplacement += panSpeedAtLocation;
                Assert.AreEqual(0, graphView.viewTransform.position.x);
                Assert.AreEqual(totalDisplacement, graphView.viewTransform.position.y);

                helpers.MouseUpEvent(new Vector2(portCenter.x, worldY + 2 * EdgeDragHelper.k_PanAreaWidth));
                needsMouseUp = false;
                yield return null;

                Assert.AreEqual(0, graphView.viewTransform.position.x);
                Assert.AreEqual(totalDisplacement, graphView.viewTransform.position.y);
            }
            finally
            {
                if (needsMouseUp)
                    helpers.MouseUpEvent(new Vector2(portCenter.x, worldY + 2 * EdgeDragHelper.k_PanAreaWidth));
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator MiniMapElementCanBeDragged()
        {
            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            MiniMap minimap = graphView.Q<MiniMap>();

            Vector2 start = minimap.worldBound.center;
            Vector2 offset = new Vector2(10, 10);

            // Move the minimap element.
            helpers.MouseDownEvent(start);
            yield return null;

            Vector2 end = start + offset;
            helpers.MouseDragEvent(start, end);
            yield return null;

            helpers.MouseUpEvent(end);
            yield return null;

            Assert.AreEqual(k_MinimapRect.x + offset.x, minimap.layout.x);
            Assert.AreEqual(k_MinimapRect.y + offset.y, minimap.layout.y);

            yield return null;
        }

        [UnityTest]
        public IEnumerator PanSpeedsAreAsExpected()
        {
            graphView.RebuildUI(GraphModel, Store);
            yield return null;

            float minSpeed = SelectionDragger.k_MinSpeedFactor * SelectionDragger.k_PanSpeed;
            float midwaySpeed = (SelectionDragger.k_MaxSpeedFactor + SelectionDragger.k_MinSpeedFactor) / 2 * SelectionDragger.k_PanSpeed;
            float maxSpeed = SelectionDragger.k_MaxSpeedFactor * SelectionDragger.k_PanSpeed;

            float maxDiagonalDistance = (float)Math.Sqrt((SelectionDragger.k_MaxSpeedFactor * SelectionDragger.k_PanSpeed) * (SelectionDragger.k_MaxSpeedFactor * SelectionDragger.k_PanSpeed) / 2);

            // safeDistance is a distance at which the component it is applied to (x or y) will have no effect on auto panning.
            float safeDistance = SelectionDragger.k_PanAreaWidth * 2;

            float worldY = graphView.ChangeCoordinatesTo(graphView.contentContainer, new Vector2(0, graphView.layout.height)).y;

            Vector2[][] testData =
            {
                // LEFT
                new[] // Test 0: Inside, in X to the left
                {
                    new Vector2(SelectionDragger.k_PanAreaWidth, safeDistance),
                    new Vector2(-minSpeed, 0)
                },
                new[] // Test 1: At the border, in X to the left
                {
                    new Vector2(0, safeDistance),
                    new Vector2(-midwaySpeed, 0)
                },
                new[] // Test 2: Outside, in X to the left
                {
                    new Vector2(-SelectionDragger.k_PanAreaWidth, safeDistance),
                    new Vector2(-maxSpeed, 0)
                },

                // RIGHT
                new[] // Test 3: Inside, in X to the right
                {
                    new Vector2(window.position.width - SelectionDragger.k_PanAreaWidth, safeDistance),
                    new Vector2(minSpeed, 0)
                },
                new[] // Test 4: At the border, in X to the right
                {
                    new Vector2(window.position.width, safeDistance),
                    new Vector2(midwaySpeed, 0)
                },
                new[] // Test 5: Outside, in X to the right
                {
                    new Vector2(window.position.width + SelectionDragger.k_PanAreaWidth, safeDistance),
                    new Vector2(maxSpeed, 0)
                },

                // TOP
                new[] // Test 6: Inside, in Y to the top
                {
                    new Vector2(safeDistance, SelectionDragger.k_PanAreaWidth),
                    new Vector2(0, -minSpeed)
                },
                new[] // Test 7: At the border, in Y to the top
                {
                    new Vector2(safeDistance, 0),
                    new Vector2(0, -midwaySpeed)
                },
                new[] // Test 8: Outside, in Y to the top
                {
                    new Vector2(safeDistance, -SelectionDragger.k_PanAreaWidth),
                    new Vector2(0, -maxSpeed)
                },

                // BOTTOM
                new[] // Test 9: Inside, in Y to the bottom
                {
                    new Vector2(safeDistance, worldY - SelectionDragger.k_PanAreaWidth),
                    new Vector2(0, minSpeed)
                },
                new[] // Test 10: At the border, in X to the bottom
                {
                    new Vector2(safeDistance, worldY),
                    new Vector2(0, midwaySpeed)
                },
                new[] // Test 11: Outside, in X to the bottom
                {
                    new Vector2(safeDistance, worldY + SelectionDragger.k_PanAreaWidth),
                    new Vector2(0, maxSpeed)
                },

                // CORNERS
                new[] // Test 12: Extreme top left corner
                {
                    new Vector2(-SelectionDragger.k_PanAreaWidth * 5, -SelectionDragger.k_PanAreaWidth * 5),
                    new Vector2(-maxDiagonalDistance, -maxDiagonalDistance)
                },
                new[] // Test 12: Extreme top right corner
                {
                    new Vector2(-SelectionDragger.k_PanAreaWidth * 5, worldY + SelectionDragger.k_PanAreaWidth * 5),
                    new Vector2(-maxDiagonalDistance, maxDiagonalDistance)
                },
                new[] // Test 12: Extreme bottom right corner
                {
                    new Vector2(window.position.width + SelectionDragger.k_PanAreaWidth * 5, worldY + SelectionDragger.k_PanAreaWidth * 5),
                    new Vector2(maxDiagonalDistance, maxDiagonalDistance)
                },
                new[] // Test 12: Extreme bottom left corner
                {
                    new Vector2(window.position.width + SelectionDragger.k_PanAreaWidth * 5, -SelectionDragger.k_PanAreaWidth * 5),
                    new Vector2(maxDiagonalDistance, -maxDiagonalDistance)
                },
            };

            Vector2 res = Vector2.zero;

            int i = 0;
            foreach (Vector2[] data in testData)
            {
                res = graphView.selectionDragger.GetEffectivePanSpeed(data[0]);
                Assert.AreEqual(data[1], res, $"Test {i} failed because ({data[1].x:R},{data[1].y:R} != ({res.x:R}, {res.y:R})");
                i++;
            }

            yield return null;
        }
    }
}
