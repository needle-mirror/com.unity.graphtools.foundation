using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    public class EdgeDragHelper
    {
        internal const int k_PanAreaWidth = 100;
        internal const int k_PanSpeed = 4;
        internal const int k_PanInterval = 10;
        internal const float k_MaxSpeedFactor = 2.5f;
        internal const float k_MaxPanSpeed = k_MaxSpeedFactor * k_PanSpeed;

        List<Port> m_CompatiblePorts;
        GhostEdgeModel m_GhostEdgeModel;
        Edge m_GhostEdge;
        public GraphView GraphView { get; }
        static NodeAdapter s_nodeAdapter = new NodeAdapter();
        readonly IStore m_Store;
        readonly EdgeConnectorListener m_Listener;
        readonly Func<IGTFGraphModel, GhostEdgeModel> m_GhostEdgeViewModelCreator;

        IVisualElementScheduledItem m_PanSchedule;
        Vector3 m_PanDiff = Vector3.zero;
        bool m_WasPanned;

        bool resetPositionOnPan { get; set; }

        public EdgeDragHelper(IStore store, GraphView graphView, EdgeConnectorListener listener, Func<IGTFGraphModel, GhostEdgeModel> ghostEdgeViewModelCreator)
        {
            m_Store = store;
            GraphView = graphView;
            m_Listener = listener;
            m_GhostEdgeViewModelCreator = ghostEdgeViewModelCreator;
            resetPositionOnPan = true;
            Reset();
        }

        public Edge CreateGhostEdge(IGTFGraphModel graphModel)
        {
            GhostEdgeModel ghostEdge;

            if (m_GhostEdgeViewModelCreator != null)
            {
                ghostEdge = m_GhostEdgeViewModelCreator.Invoke(graphModel);
            }
            else
            {
                ghostEdge = new GhostEdgeModel(graphModel);
            }
            var ui = ghostEdge.CreateUI<Edge>(GraphView, m_Store);
            return ui;
        }

        GhostEdgeModel m_EdgeCandidateModel;
        Edge m_EdgeCandidate;
        public GhostEdgeModel edgeCandidateModel => m_EdgeCandidateModel;

        public void CreateEdgeCandidate(IGTFGraphModel graphModel)
        {
            m_EdgeCandidate = CreateGhostEdge(graphModel);
            m_EdgeCandidateModel = m_EdgeCandidate.EdgeModel as GhostEdgeModel;
        }

        void ClearEdgeCandidate()
        {
            m_EdgeCandidateModel = null;
            m_EdgeCandidate = null;
        }

        public IGTFPortModel draggedPort { get; set; }
        public Edge originalEdge { get; set; }

        public void Reset(bool didConnect = false)
        {
            if (m_CompatiblePorts != null)
            {
                // Reset the highlights.
                GraphView.ports.ForEach((p) =>
                {
                    p.SetEnabled(true);
                    p.Highlighted = false;
                });
                m_CompatiblePorts = null;
            }

            if (m_GhostEdge != null)
            {
                GraphView.RemoveElement(m_GhostEdge);
            }

            if (m_EdgeCandidate != null)
            {
                GraphView.RemoveElement(m_EdgeCandidate);
            }

            if (m_WasPanned)
            {
                if (!resetPositionOnPan || didConnect)
                {
                    Vector3 p = GraphView.contentViewContainer.transform.position;
                    Vector3 s = GraphView.contentViewContainer.transform.scale;
                    GraphView.UpdateViewTransform(p, s);
                }
            }

            m_PanSchedule?.Pause();

            if (draggedPort != null && !didConnect)
            {
                var portUI = draggedPort.GetUI<Port>(GraphView);
                if (portUI != null)
                    portUI.WillConnect = false;

                draggedPort = null;
            }

            m_GhostEdge = null;
            ClearEdgeCandidate();
        }

        public bool HandleMouseDown(MouseDownEvent evt)
        {
            Vector2 mousePosition = evt.mousePosition;

            if (draggedPort == null || edgeCandidateModel == null)
            {
                return false;
            }

            if (m_EdgeCandidate == null)
                return false;

            if (m_EdgeCandidate.parent == null)
            {
                GraphView.AddElement(m_EdgeCandidate);
            }

            bool startFromOutput = draggedPort.Direction == Direction.Output;

            edgeCandidateModel.EndPoint = mousePosition;
            m_EdgeCandidate.SetEnabled(false);

            if (startFromOutput)
            {
                edgeCandidateModel.FromPort = draggedPort;
                edgeCandidateModel.ToPort = null;
            }
            else
            {
                edgeCandidateModel.FromPort = null;
                edgeCandidateModel.ToPort = draggedPort;
            }

            var portUI = draggedPort.GetUI<Port>(GraphView);
            if (portUI != null)
                portUI.WillConnect = true;

            m_CompatiblePorts = GraphView.GetCompatiblePorts(draggedPort, s_nodeAdapter);

            // Only light compatible anchors when dragging an edge.
            GraphView.ports.ForEach((p) =>
            {
                p.SetEnabled(false);
                p.Highlighted = false;
            });

            foreach (Port compatiblePort in m_CompatiblePorts)
            {
                compatiblePort.SetEnabled(true);
                compatiblePort.Highlighted = true;
            }

            m_EdgeCandidate.UpdateFromModel();

            if (m_PanSchedule == null)
            {
                m_PanSchedule = GraphView.schedule.Execute(Pan).Every(k_PanInterval).StartingIn(k_PanInterval);
                m_PanSchedule.Pause();
            }

            m_WasPanned = false;

            m_EdgeCandidate.layer = Int32.MaxValue;

            return true;
        }

        Vector2 GetEffectivePanSpeed(Vector2 mousePos)
        {
            Vector2 effectiveSpeed = Vector2.zero;

            if (mousePos.x <= k_PanAreaWidth)
                effectiveSpeed.x = -(((k_PanAreaWidth - mousePos.x) / k_PanAreaWidth) + 0.5f) * k_PanSpeed;
            else if (mousePos.x >= GraphView.contentContainer.layout.width - k_PanAreaWidth)
                effectiveSpeed.x = (((mousePos.x - (GraphView.contentContainer.layout.width - k_PanAreaWidth)) / k_PanAreaWidth) + 0.5f) * k_PanSpeed;

            if (mousePos.y <= k_PanAreaWidth)
                effectiveSpeed.y = -(((k_PanAreaWidth - mousePos.y) / k_PanAreaWidth) + 0.5f) * k_PanSpeed;
            else if (mousePos.y >= GraphView.contentContainer.layout.height - k_PanAreaWidth)
                effectiveSpeed.y = (((mousePos.y - (GraphView.contentContainer.layout.height - k_PanAreaWidth)) / k_PanAreaWidth) + 0.5f) * k_PanSpeed;

            effectiveSpeed = Vector2.ClampMagnitude(effectiveSpeed, k_MaxPanSpeed);

            return effectiveSpeed;
        }

        public void HandleMouseMove(MouseMoveEvent evt)
        {
            var ve = (VisualElement)evt.target;
            Vector2 gvMousePos = ve.ChangeCoordinatesTo(GraphView.contentContainer, evt.localMousePosition);
            m_PanDiff = GetEffectivePanSpeed(gvMousePos);

            if (m_PanDiff != Vector3.zero)
            {
                m_PanSchedule.Resume();
            }
            else
            {
                m_PanSchedule.Pause();
            }

            Vector2 mousePosition = evt.mousePosition;

            edgeCandidateModel.EndPoint = mousePosition;
            m_EdgeCandidate.UpdateFromModel();

            // Draw ghost edge if possible port exists.
            Port endPort = GetEndPort(mousePosition);

            if (endPort != null)
            {
                if (m_GhostEdge == null)
                {
                    m_GhostEdge = CreateGhostEdge(endPort.PortModel.GraphModel);
                    m_GhostEdgeModel = m_GhostEdge.EdgeModel as GhostEdgeModel;

                    m_GhostEdge.pickingMode = PickingMode.Ignore;
                    GraphView.AddElement(m_GhostEdge);
                }

                Debug.Assert(m_GhostEdgeModel != null);

                if (edgeCandidateModel.FromPort == null)
                {
                    m_GhostEdgeModel.ToPort = edgeCandidateModel.ToPort;
                    var portUI = m_GhostEdgeModel?.FromPort?.GetUI<Port>(GraphView);
                    if (portUI != null)
                        portUI.WillConnect = false;
                    m_GhostEdgeModel.FromPort = endPort.PortModel;
                    endPort.WillConnect = true;
                }
                else
                {
                    var portUI = m_GhostEdgeModel?.ToPort?.GetUI<Port>(GraphView);
                    if (portUI != null)
                        portUI.WillConnect = false;
                    m_GhostEdgeModel.ToPort = endPort.PortModel;
                    endPort.WillConnect = true;
                    m_GhostEdgeModel.FromPort = edgeCandidateModel.FromPort;
                }

                m_GhostEdge.UpdateFromModel();
            }
            else if (m_GhostEdge != null && m_GhostEdgeModel != null)
            {
                if (edgeCandidateModel.ToPort == null)
                {
                    var portUI = m_GhostEdgeModel?.ToPort?.GetUI<Port>(GraphView);
                    if (portUI != null)
                        portUI.WillConnect = false;
                }
                else
                {
                    var portUI = m_GhostEdgeModel?.FromPort?.GetUI<Port>(GraphView);
                    if (portUI != null)
                        portUI.WillConnect = false;
                }
                GraphView.RemoveElement(m_GhostEdge);
                m_GhostEdgeModel.ToPort = null;
                m_GhostEdgeModel.FromPort = null;
                m_GhostEdgeModel = null;
                m_GhostEdge = null;
            }
        }

        void Pan(TimerState ts)
        {
            GraphView.viewTransform.position -= m_PanDiff;
            edgeCandidateModel.GetUI<Edge>(GraphView)?.UpdateFromModel();
            m_WasPanned = true;
        }

        public void HandleMouseUp(MouseUpEvent evt)
        {
            bool didConnect = false;

            Vector2 mousePosition = evt.mousePosition;

            // Reset the highlights.
            GraphView.ports.ForEach((p) =>
            {
                p.SetEnabled(true);
                p.Highlighted = false;
            });

            Port portUI;
            // Clean up ghost edges.
            if (m_GhostEdgeModel != null)
            {
                portUI = m_GhostEdgeModel.ToPort?.GetUI<Port>(GraphView);
                if (portUI != null)
                    portUI.WillConnect = false;

                portUI = m_GhostEdgeModel.FromPort?.GetUI<Port>(GraphView);
                if (portUI != null)
                    portUI.WillConnect = false;

                GraphView.RemoveElement(m_GhostEdge);
                m_GhostEdgeModel.ToPort = null;
                m_GhostEdgeModel.FromPort = null;
                m_GhostEdgeModel = null;
                m_GhostEdge = null;
            }

            Port endPort = GetEndPort(mousePosition);

            if (endPort == null && m_Listener != null)
            {
                m_Listener.OnDropOutsidePort(m_Store, m_EdgeCandidate, mousePosition, originalEdge);
            }

            m_EdgeCandidate.SetEnabled(true);

            portUI = edgeCandidateModel?.ToPort?.GetUI<Port>(GraphView);
            if (portUI != null)
                portUI.WillConnect = false;

            portUI = edgeCandidateModel?.FromPort?.GetUI<Port>(GraphView);
            if (portUI != null)
                portUI.WillConnect = false;

            // If it is an existing valid edge then delete and notify the model (using DeleteElements()).
            if (edgeCandidateModel?.ToPort != null && edgeCandidateModel?.FromPort != null)
            {
                // Save the current input and output before deleting the edge as they will be reset
                var oldInput = edgeCandidateModel.ToPort;
                var oldOutput = edgeCandidateModel.FromPort;

                GraphView.DeleteElements(new[] { m_EdgeCandidate as GraphElement });

                // Restore the previous input and output
                edgeCandidateModel.ToPort = oldInput;
                edgeCandidateModel.FromPort = oldOutput;
            }
            // otherwise, if it is an temporary edge then just remove it as it is not already known my the model
            else
            {
                GraphView.RemoveElement(m_EdgeCandidate);
            }

            if (endPort != null)
            {
                if (edgeCandidateModel != null)
                {
                    if (endPort.PortModel.Direction == Direction.Output)
                        edgeCandidateModel.FromPort = endPort.PortModel;
                    else
                        edgeCandidateModel.ToPort = endPort.PortModel;
                }

                m_Listener.OnDrop(m_Store, m_EdgeCandidate, originalEdge);
                didConnect = true;
            }
            else if (edgeCandidateModel != null)
            {
                edgeCandidateModel.FromPort = null;
                edgeCandidateModel.ToPort = null;
            }

            m_EdgeCandidate?.ResetLayer();

            ClearEdgeCandidate();
            m_CompatiblePorts = null;
            Reset(didConnect);

            originalEdge = null;
        }

        Port GetEndPort(Vector2 mousePosition)
        {
            Port endPort = null;

            foreach (Port compatiblePort in m_CompatiblePorts)
            {
                if (compatiblePort.resolvedStyle.visibility != Visibility.Visible)
                    continue;

                Rect bounds = compatiblePort.worldBound;
                float hitboxExtraPadding = bounds.height;

                if (compatiblePort.PortModel.Orientation == Orientation.Horizontal)
                {
                    // Add extra padding for mouse check to the left of input port or right of output port.
                    if (compatiblePort.PortModel.Direction == Direction.Input)
                    {
                        // Move bounds to the left by hitboxExtraPadding and increase width
                        // by hitboxExtraPadding.
                        bounds.x -= hitboxExtraPadding;
                        bounds.width += hitboxExtraPadding;
                    }
                    else if (compatiblePort.PortModel.Direction == Direction.Output)
                    {
                        // Just add hitboxExtraPadding to the width.
                        bounds.width += hitboxExtraPadding;
                    }
                }

                // Check if mouse is over port.
                if (bounds.Contains(mousePosition))
                {
                    endPort = compatiblePort;
                    break;
                }
            }

            return endPort;
        }
    }
}
