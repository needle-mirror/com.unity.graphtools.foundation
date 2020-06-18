using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    class SnapService
    {
        internal enum SnapReference
        {
            LeftEdge,
            HorizontalCenter,
            RightEdge,
            TopEdge,
            VerticalCenter,
            BottomEdge
        }
        internal class SnapResult
        {
            public Port sourcePort { get; set; }
            public Port snappablePort { get; set; }
            public Orientation portOrientation { get; set; }
            public Rect sourceRect { get; set; }
            public SnapReference sourceReference { get; set; }
            public Rect snappableRect { get; set; }
            public SnapReference snappableReference { get; set; }
            public float offset { get; set; }
            public float distance { get { return Math.Abs(offset); } }
            public Line indicatorLine;
            public SnapResult()
            {
            }
        }

        const float k_DefaultSnapDistance = 8.0f;
        float m_CurrentScale = 1.0f;
        GraphView m_GraphView;
        List<Rect> m_SnappableRects = new List<Rect>();
        List<Edge> m_ConnectedEdges = new List<Edge>();
        Dictionary<Port, Vector2> m_ConnectedPortsPos = new Dictionary<Port, Vector2>();

        public bool snapToBordersIsActive { get; private set; }
        public bool snapToPortIsActive { get; private set; }

        public float snapDistance { get; set; }
        public SnapService()
        {
            snapDistance = k_DefaultSnapDistance;
        }

        static float GetPos(Rect rect, SnapReference reference)
        {
            switch (reference)
            {
                case SnapReference.LeftEdge:
                    return rect.x;
                case SnapReference.HorizontalCenter:
                    return rect.center.x;
                case SnapReference.RightEdge:
                    return rect.xMax;
                case SnapReference.TopEdge:
                    return rect.y;
                case SnapReference.VerticalCenter:
                    return rect.center.y;
                case SnapReference.BottomEdge:
                    return rect.yMax;
                default:
                    return 0;
            }
        }

        public virtual void BeginSnapToBorders(List<Rect> snappableRects)
        {
            if (snapToBordersIsActive)
            {
                throw new InvalidOperationException("SnapService.BeginSnapToBorders: Already active. Call EndSnapToBorders() first.");
            }
            snapToBordersIsActive = true;
            m_SnappableRects = new List<Rect>(snappableRects);
        }

        public virtual void BeginSnapToPort(GraphView graphView, List<Edge> connectedEdges, Dictionary<Port, Vector2> connectedPortPositions)
        {
            if (snapToPortIsActive)
            {
                throw new InvalidOperationException("SnapService.BeginSnapPort: Already active. Call EndSnapToPort() first.");
            }
            snapToPortIsActive = true;
            m_GraphView = graphView;
            m_ConnectedEdges = new List<Edge>(connectedEdges);
            m_ConnectedPortsPos = new Dictionary<Port, Vector2>(connectedPortPositions);
        }

        public void UpdateSnapRects(List<Rect> snappableRects)
        {
            m_SnappableRects = snappableRects;
        }

        public Rect GetSnappedRectToBorders(Rect sourceRect, out List<SnapResult> results, float scale = 1.0f)
        {
            if (!snapToBordersIsActive)
            {
                throw new InvalidOperationException("SnapService.GetSnappedRectToBorders: Already active. Call BeginSnapToBorders() first.");
            }
            Rect snappedRect = sourceRect;
            m_CurrentScale = scale;

            results = GetClosestSnapElements(sourceRect);
            foreach (SnapResult result in results)
            {
                ApplySnapToBordersResult(sourceRect, ref snappedRect, result);
                result.indicatorLine = GetSnapLine(snappedRect, result.sourceReference, result.snappableRect, result.snappableReference);
            }

            return snappedRect;
        }

        public Rect GetSnappedRectToPort(Vector2 mousePanningDelta, Rect sourceRect, Node selectedNode, float scale = 1.0f)
        {
            if (!snapToPortIsActive)
            {
                throw new InvalidOperationException("SnapService.GetSnappedRectToPort: Already active. Call BeginSnapToPort() first.");
            }

            m_CurrentScale = scale;
            Rect snappedRect = sourceRect;
            SnapResult chosenResult = GetClosestSnapToPortResult(selectedNode, mousePanningDelta, sourceRect);

            if (chosenResult != null)
            {
                var adjustedSourceRect = GetAdjustedSourceRect(chosenResult, sourceRect, mousePanningDelta);
                snappedRect = adjustedSourceRect;

                ApplySnapToPortResult(adjustedSourceRect, ref snappedRect, chosenResult);
            }

            return snappedRect;
        }

        public virtual void EndSnapToBorders()
        {
            if (!snapToBordersIsActive)
            {
                throw new InvalidOperationException("SnapService.EndSnapToBorders: Already active. Call BeginSnapToBorders() first.");
            }
            m_SnappableRects.Clear();

            snapToBordersIsActive = false;
        }

        virtual public void EndSnapToPort()
        {
            if (!snapToPortIsActive)
            {
                throw new InvalidOperationException("SnapService.EndSnapToPort: Already active. Call BeginSnapToPort() first.");
            }
            m_ConnectedEdges.Clear();
            m_ConnectedPortsPos.Clear();

            snapToPortIsActive = false;
        }

        SnapResult GetClosestSnapElement(Rect sourceRect, SnapReference sourceRef, Rect snappableRect, SnapReference startReference, SnapReference centerReference, SnapReference endReference)
        {
            float sourcePos = GetPos(sourceRect, sourceRef);
            float offsetStart = sourcePos - GetPos(snappableRect, startReference);
            float offsetEnd = sourcePos - GetPos(snappableRect, endReference);
            float minOffset = offsetStart;
            SnapReference minSnappableReference = startReference;
            if (Math.Abs(minOffset) > Math.Abs(offsetEnd))
            {
                minOffset = offsetEnd;
                minSnappableReference = endReference;
            }
            SnapResult minResult = new SnapResult
            {
                sourceRect = sourceRect,
                sourceReference = sourceRef,
                snappableRect = snappableRect,
                snappableReference = minSnappableReference,
                offset = minOffset
            };
            if (minResult.distance <= snapDistance * 1 / m_CurrentScale)
                return minResult;
            else
                return null;
        }

        SnapResult GetClosestSnapElement(Rect sourceRect, SnapReference sourceRef, SnapReference startReference, SnapReference centerReference, SnapReference endReference)
        {
            SnapResult minResult = null;
            float minDistance = float.MaxValue;
            foreach (Rect snappableRect in m_SnappableRects)
            {
                SnapResult result = GetClosestSnapElement(sourceRect, sourceRef, snappableRect, startReference, centerReference, endReference);
                if (result != null && minDistance > result.distance)
                {
                    minDistance = result.distance;
                    minResult = result;
                }
            }
            return minResult;
        }

        List<SnapResult> GetClosestSnapElements(Rect sourceRect, Orientation orientation)
        {
            SnapReference startReference = orientation == Orientation.Horizontal ? SnapReference.LeftEdge : SnapReference.TopEdge;
            SnapReference centerReference = orientation == Orientation.Horizontal ? SnapReference.HorizontalCenter : SnapReference.VerticalCenter;
            SnapReference endReference = orientation == Orientation.Horizontal ? SnapReference.RightEdge : SnapReference.BottomEdge;
            List<SnapResult> results = new List<SnapResult>(3);
            SnapResult result = GetClosestSnapElement(sourceRect, startReference, startReference, centerReference, endReference);
            if (result != null)
                results.Add(result);
            result = GetClosestSnapElement(sourceRect, centerReference, startReference, centerReference, endReference);
            if (result != null)
                results.Add(result);
            result = GetClosestSnapElement(sourceRect, endReference, startReference, centerReference, endReference);
            if (result != null)
                results.Add(result);
            // Look for the minimum
            if (results.Count > 0)
            {
                results.Sort((a, b) => a.distance.CompareTo(b.distance));
                float minDistance = results[0].distance;
                results.RemoveAll(r => Math.Abs(r.distance - minDistance) > 0.01f);
            }
            return results;
        }

        List<SnapResult> GetClosestSnapElements(Rect sourceRect)
        {
            List<SnapResult> snapResults = GetClosestSnapElements(sourceRect, Orientation.Horizontal);
            return snapResults.Union(GetClosestSnapElements(sourceRect, Orientation.Vertical)).ToList();
        }

        SnapResult GetClosestSnapToPortResult(Node selectedNode, Vector2 mousePanningDelta, Rect sourceRect)
        {
            List<SnapResult> results = GetSnapToPortResults(selectedNode);

            float smallestDraggedDistanceFromNode = Single.MaxValue;
            SnapResult closestResult = null;
            foreach (SnapResult result in results)
            {
                // We have to consider the mouse and panning delta to estimate the distance when the node is being dragged
                float draggedDistanceFromNode = Math.Abs(result.offset - (result.portOrientation == Orientation.Horizontal ? mousePanningDelta.y : mousePanningDelta.x));
                bool isSnapping = IsSnappingToPort(draggedDistanceFromNode);

                if (isSnapping && smallestDraggedDistanceFromNode > draggedDistanceFromNode)
                {
                    smallestDraggedDistanceFromNode = draggedDistanceFromNode;
                    closestResult = result;
                }
            }

            return closestResult;
        }

        Rect GetAdjustedSourceRect(SnapResult result, Rect sourceRect, Vector2 mousePanningDelta)
        {
            Rect adjustedSourceRect = sourceRect;
            // We only want the mouse delta position and panning info on the axis that is not snapping
            if (result.portOrientation == Orientation.Horizontal)
            {
                adjustedSourceRect.y += mousePanningDelta.y;
            }
            else
            {
                adjustedSourceRect.x += mousePanningDelta.x;
            }

            return adjustedSourceRect;
        }

        List<SnapResult> GetSnapToPortResults(Node selectedNode)
        {
            List<SnapResult> results = new List<SnapResult>();

            foreach (Edge edge in m_ConnectedEdges)
            {
                SnapResult result = GetSnapToPortResult(edge, selectedNode);

                if (result != null)
                {
                    results.Add(result);
                }
            }
            return results;
        }

        SnapResult GetSnapToPortResult(Edge edge, Node selectedNode)
        {
            Port sourcePort = null;
            Port snappablePort = null;

            if (edge.Output.NodeModel == selectedNode.NodeModel)
            {
                sourcePort = edge.Output.GetUI<Port>(m_GraphView);
                snappablePort = edge.Input.GetUI<Port>(m_GraphView);
            }
            else if (edge.Input.NodeModel == selectedNode.NodeModel)
            {
                sourcePort = edge.Input.GetUI<Port>(m_GraphView);
                snappablePort = edge.Output.GetUI<Port>(m_GraphView);
            }

            // We don't want to snap non existing ports and ports with different orientations (to be determined)
            if (sourcePort == null || snappablePort == null || sourcePort.Orientation != snappablePort.Orientation)
            {
                return null;
            }

            float offset;
            if (snappablePort.Orientation == Orientation.Horizontal)
            {
                offset = m_ConnectedPortsPos[sourcePort].y - m_ConnectedPortsPos[snappablePort].y;
            }
            else
            {
                offset = m_ConnectedPortsPos[sourcePort].x - m_ConnectedPortsPos[snappablePort].x;
            }

            SnapResult minResult = new SnapResult
            {
                sourcePort = sourcePort,
                snappablePort = snappablePort,
                portOrientation = snappablePort.Orientation,
                offset = offset
            };

            return minResult;
        }

        bool IsSnappingToPort(float draggedDistanceFromNode)
        {
            bool isSnapping = draggedDistanceFromNode <= snapDistance * 1 / m_CurrentScale;

            return isSnapping;
        }

        Line GetSnapLine(Rect r, SnapReference reference)
        {
            Vector2 start = Vector2.zero;
            Vector2 end = Vector2.zero;
            switch (reference)
            {
                case SnapReference.LeftEdge:
                    start = r.position;
                    end = new Vector2(r.x, r.yMax);
                    break;
                case SnapReference.HorizontalCenter:
                    start = r.center;
                    end = start;
                    break;
                case SnapReference.RightEdge:
                    start = new Vector2(r.xMax, r.yMin);
                    end = new Vector2(r.xMax, r.yMax);
                    break;
                case SnapReference.TopEdge:
                    start = r.position;
                    end = new Vector2(r.xMax, r.yMin);
                    break;
                case SnapReference.VerticalCenter:
                    start = r.center;
                    end = start;
                    break;
                default: // case SnapReference.BottomEdge:
                    start = new Vector2(r.x, r.yMax);
                    end = new Vector2(r.xMax, r.yMax);
                    break;
            }
            return new Line(start, end);
        }

        Line GetSnapLine(Rect r1, SnapReference reference1, Rect r2, SnapReference reference2)
        {
            bool horizontal = reference1 <= SnapReference.RightEdge;
            Line line1 = GetSnapLine(r1, reference1);
            Line line2 = GetSnapLine(r2, reference2);
            Vector2 p11 = line1.Start;
            Vector2 p12 = line1.End;
            Vector2 p21 = line2.Start;
            Vector2 p22 = line2.End;
            Vector2 start = Vector2.zero;
            Vector2 end = Vector2.zero;

            if (horizontal)
            {
                float x = p21.x;
                float yMin = Math.Min(p22.y, Math.Min(p21.y, Math.Min(p11.y, p12.y)));
                float yMax = Math.Max(p22.y, Math.Max(p21.y, Math.Max(p11.y, p12.y)));
                start = new Vector2(x, yMin);
                end = new Vector2(x, yMax);
            }
            else
            {
                float y = p22.y;
                float xMin = Math.Min(p22.x, Math.Min(p21.x, Math.Min(p11.x, p12.x)));
                float xMax = Math.Max(p22.x, Math.Max(p21.x, Math.Max(p11.x, p12.x)));
                start = new Vector2(xMin, y);
                end = new Vector2(xMax, y);
            }
            return new Line(start, end);
        }

        void ApplySnapToPortResult(Rect sourceRect, ref Rect r1, SnapResult result)
        {
            if (result.portOrientation == Orientation.Horizontal)
            {
                r1.y = sourceRect.y - result.offset;
            }
            else
            {
                r1.x = sourceRect.x - result.offset;
            }
        }

        void ApplySnapToBordersResult(Rect sourceRect, ref Rect r1, SnapResult result)
        {
            if (result.snappableReference <= SnapReference.RightEdge)
            {
                r1.x = sourceRect.x - result.offset;
            }
            else
            {
                r1.y = sourceRect.y - result.offset;
            }
        }
    }
}
