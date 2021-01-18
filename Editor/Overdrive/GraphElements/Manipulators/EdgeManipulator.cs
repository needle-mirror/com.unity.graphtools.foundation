using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class EdgeManipulator : MouseManipulator
    {
        bool m_Active;
        Edge m_Edge;
        Vector2 m_PressPos;
        EdgeDragHelper m_ConnectedEdgeDragHelper;
        IPortModel m_DetachedPort;
        bool m_DetachedFromInputPort;
        static int s_StartDragDistance = 10;
        MouseDownEvent m_LastMouseDownEvent;

        public EdgeManipulator()
        {
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });

            Reset();
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            target.RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            target.UnregisterCallback<KeyDownEvent>(OnKeyDown);
        }

        void Reset()
        {
            m_Active = false;
            m_Edge = null;
            m_ConnectedEdgeDragHelper = null;
            m_DetachedPort = null;
            m_DetachedFromInputPort = false;
        }

        protected void OnMouseDown(MouseDownEvent evt)
        {
            if (m_Active)
            {
                evt.StopImmediatePropagation();
                return;
            }

            if (!CanStartManipulation(evt))
            {
                return;
            }

            m_Edge = (evt.target as VisualElement)?.GetFirstOfType<Edge>();

            m_PressPos = evt.mousePosition;
            target.CaptureMouse();
            evt.StopPropagation();
            m_LastMouseDownEvent = evt;
        }

        protected void OnMouseMove(MouseMoveEvent evt)
        {
            // If the left mouse button is not down then return
            if (m_Edge == null)
            {
                return;
            }

            evt.StopPropagation();

            bool alreadyDetached = (m_DetachedPort != null);

            // If one end of the edge is not already detached then
            if (!alreadyDetached)
            {
                float delta = (evt.mousePosition - m_PressPos).sqrMagnitude;

                if (delta < (s_StartDragDistance * s_StartDragDistance))
                {
                    return;
                }

                var graphView = m_Edge.GraphView;
                var outputPortUI = m_Edge.Output.GetUI<Port>(graphView);
                var inputPortUI = m_Edge.Input.GetUI<Port>(graphView);

                if (outputPortUI == null || inputPortUI == null)
                {
                    return;
                }

                // Determine which end is the nearest to the mouse position then detach it.
                Vector2 outputPos = new Vector2(outputPortUI.GetGlobalCenter().x, outputPortUI.GetGlobalCenter().y);
                Vector2 inputPos = new Vector2(inputPortUI.GetGlobalCenter().x, inputPortUI.GetGlobalCenter().y);

                float distanceFromOutput = (m_PressPos - outputPos).sqrMagnitude;
                float distanceFromInput = (m_PressPos - inputPos).sqrMagnitude;

                if (distanceFromInput > 50 * 50 && distanceFromOutput > 50 * 50)
                {
                    return;
                }

                m_DetachedFromInputPort = distanceFromInput < distanceFromOutput;

                IPortModel connectedPort;
                Port connectedPortUI;

                if (m_DetachedFromInputPort)
                {
                    connectedPort = m_Edge.Output;
                    connectedPortUI = outputPortUI;

                    m_DetachedPort = m_Edge.Input;
                }
                else
                {
                    connectedPort = m_Edge.Input;
                    connectedPortUI = inputPortUI;

                    m_DetachedPort = m_Edge.Output;
                }

                // Use the edge drag helper of the still connected port
                m_ConnectedEdgeDragHelper = connectedPortUI.EdgeConnector.edgeDragHelper;
                m_ConnectedEdgeDragHelper.originalEdge = m_Edge;
                m_ConnectedEdgeDragHelper.draggedPort = connectedPort;
                m_ConnectedEdgeDragHelper.CreateEdgeCandidate(connectedPort.GraphModel);
                m_ConnectedEdgeDragHelper.edgeCandidateModel.EndPoint = evt.mousePosition;

                // Redirect the last mouse down event to active the drag helper

                if (m_ConnectedEdgeDragHelper.HandleMouseDown(m_LastMouseDownEvent))
                {
                    m_Active = true;
                }
                else
                {
                    Reset();
                }

                m_LastMouseDownEvent = null;
            }

            if (m_Active)
                m_ConnectedEdgeDragHelper.HandleMouseMove(evt);
        }

        protected void OnMouseUp(MouseUpEvent evt)
        {
            if (CanStopManipulation(evt))
            {
                target.ReleaseMouse();
                if (m_Active)
                {
                    m_ConnectedEdgeDragHelper.HandleMouseUp(evt);
                }
                Reset();
                evt.StopPropagation();
            }
        }

        protected void OnKeyDown(KeyDownEvent evt)
        {
            if (m_Active)
            {
                if (evt.keyCode == KeyCode.Escape)
                {
                    m_ConnectedEdgeDragHelper.Reset();
                    Reset();
                    target.ReleaseMouse();
                    evt.StopPropagation();
                }
            }
        }
    }
}
