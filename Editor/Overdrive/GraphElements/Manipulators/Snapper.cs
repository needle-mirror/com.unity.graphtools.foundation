using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    internal class Snapper
    {
        SnapService m_Service;
        LineView m_LineView;
        GraphView m_GraphView;
        public bool snapToBordersIsActive { get { return m_Service != null ? m_Service.snapToBordersIsActive : false; } }
        public bool snapToPortIsActive { get { return m_Service != null ? m_Service.snapToPortIsActive : false; } }

        public Snapper()
        {
        }

        internal void BeginSnapToPort(GraphView graphView, Node selectedNode)
        {
            if (m_Service == null)
            {
                m_Service = new SnapService();
            }

            m_GraphView = graphView;

            var connectedEdges = GetConnectedEdges(selectedNode);
            var connectedPortPositions = GetConnectedPortPositions(connectedEdges);
            m_Service.BeginSnapToPort(graphView, connectedEdges, connectedPortPositions);
        }

        internal void BeginSnapToBorders(GraphView graphView, GraphElement selectedElement)
        {
            if (m_Service == null)
            {
                m_Service = new SnapService();
            }

            if (m_LineView == null)
            {
                m_LineView = new LineView();
            }

            m_GraphView = graphView;
            m_GraphView.Add(m_LineView);
            m_LineView.SetLayout(new Rect(0, 0, m_GraphView.layout.width, m_GraphView.layout.height));

            var notSelectedElementRects = GetNotSelectedElementRectsInView(selectedElement);
            m_Service.BeginSnapToBorders(notSelectedElementRects);
        }

        public List<Edge> GetConnectedEdges(Node selectedNode)
        {
            List<Edge> connectedEdges = new List<Edge>();

            foreach (Edge edge in m_GraphView.edges.ToList())
            {
                if (edge.Output.NodeModel == selectedNode.NodeModel || edge.Input.NodeModel == selectedNode.NodeModel)
                {
                    connectedEdges.Add(edge);
                }
            }

            return connectedEdges;
        }

        internal List<Rect> GetNotSelectedElementRectsInView(GraphElement selectedElement)
        {
            List<Rect> notSelectedElementRects = new List<Rect>();
            List<GraphElement> ignoredElements = m_GraphView.selection.OfType<GraphElement>().ToList();

            // Consider only the visible nodes.
            Rect rectToFit = m_GraphView.layout;

            foreach (GraphElement element in m_GraphView.graphElements.ToList())
            {
                if (selectedElement is Placemat && element.layout.Overlaps(((Placemat)selectedElement).layout))
                {
                    // If the selected element is a placemat, we do not consider the elements under it
                    ignoredElements.Add(element);
                }
                else if (element is Edge)
                {
                    // Don't consider edges
                    ignoredElements.Add(element);
                }
                else if (!element.visible)
                {
                    // Don't consider not visible elements
                    ignoredElements.Add(element);
                }
                else if (!element.IsSelected(m_GraphView) && !(ignoredElements.Contains(element)))
                {
                    var localSelRect = m_GraphView.ChangeCoordinatesTo(element, rectToFit);
                    if (element.Overlaps(localSelRect))
                    {
                        Rect geometryInContentViewContainerSpace = (element).parent.ChangeCoordinatesTo(m_GraphView.contentViewContainer, (element).GetPosition());
                        notSelectedElementRects.Add(geometryInContentViewContainerSpace);
                    }
                }
            }

            return notSelectedElementRects;
        }

        internal Rect GetSnappedRectToBorders(Rect sourceRect, GraphElement selectedElement, float scale = 1.0f)
        {
            List<SnapService.SnapResult> results;

            Rect snappedRect = m_Service.GetSnappedRectToBorders(sourceRect, out results, scale);

            m_Service.UpdateSnapRects(GetNotSelectedElementRectsInView(selectedElement));
            m_LineView.lines.Clear();

            foreach (SnapService.SnapResult result in results)
            {
                m_LineView.lines.Add(result.indicatorLine);
            }

            m_LineView.MarkDirtyRepaint();

            return snappedRect;
        }

        internal Rect GetSnappedRectToPort(Rect sourceRect, Node selectedNode, Vector2 mousePanningDelta, float scale = 1.0f)
        {
            Rect snappedRect = m_Service.GetSnappedRectToPort(mousePanningDelta, sourceRect, selectedNode, scale);

            return snappedRect;
        }

        internal void EndSnapToPort()
        {
            m_Service.EndSnapToPort();
        }

        internal void EndSnapToBorders()
        {
            m_LineView.lines.Clear();
            m_LineView.Clear();
            m_LineView.RemoveFromHierarchy();
            m_Service.EndSnapToBorders();
        }

        internal void ClearSnapLines()
        {
            m_LineView.lines.Clear();
            m_LineView.MarkDirtyRepaint();
        }

        Dictionary<Port, Vector2> GetConnectedPortPositions(List<Edge> edges)
        {
            Dictionary<Port, Vector2> connectedPortsOriginalPos = new Dictionary<Port, Vector2>();
            foreach (var edge in edges)
            {
                Port inputPort = edge.Input.GetUI<Port>(m_GraphView);
                Port outputPort = edge.Output.GetUI<Port>(m_GraphView);

                if (inputPort != null)
                {
                    Vector2 inputPortPosInContentViewContainerSpace = inputPort.parent.ChangeCoordinatesTo(m_GraphView.contentViewContainer, inputPort.layout.center);
                    if (!connectedPortsOriginalPos.ContainsKey(inputPort))
                    {
                        connectedPortsOriginalPos.Add(inputPort, inputPortPosInContentViewContainerSpace);
                    }
                }

                if (outputPort != null)
                {
                    Vector2 outputPortPosInContentViewContainerSpace = outputPort.parent.ChangeCoordinatesTo(m_GraphView.contentViewContainer, outputPort.layout.center);
                    if (!connectedPortsOriginalPos.ContainsKey(outputPort))
                    {
                        connectedPortsOriginalPos.Add(outputPort, outputPortPosInContentViewContainerSpace);
                    }
                }
            }

            return connectedPortsOriginalPos;
        }
    }
}
