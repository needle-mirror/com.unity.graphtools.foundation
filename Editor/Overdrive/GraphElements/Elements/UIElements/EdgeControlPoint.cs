using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class EdgeControlPoint : VisualElement
    {
        public static readonly string k_ClassName = "ge-edge-control-point";

        public static readonly string k_RemoveControlPointMenuItem = "Remove control point";

        EdgeControl m_EdgeControl;

        IEditableEdge m_EdgeModel;

        int m_ControlPointIndex;

        bool m_DraggingControlPoint;

        bool m_DraggingTightness;

        Vector2 m_OriginalElementPosition;

        float m_OriginalTightness;

        Vector2 m_OriginalPointerPosition;

        protected ContextualMenuManipulator m_ContextualMenuManipulator;

        public EdgeControlPoint(EdgeControl edgeControl, IEditableEdge edgeModel, int controlPointIndex)
        {
            m_EdgeControl = edgeControl;
            m_EdgeModel = edgeModel;
            m_ControlPointIndex = controlPointIndex;

            AddToClassList(k_ClassName);

            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);

            style.position = Position.Absolute;

            m_ContextualMenuManipulator = new ContextualMenuManipulator(BuildContextualMenu);
            this.AddManipulator(m_ContextualMenuManipulator);
        }

        void OnPointerDown(PointerDownEvent e)
        {
            if (!e.isPrimary || e.button != 0)
                return;

            m_OriginalPointerPosition = this.ChangeCoordinatesTo(parent, e.localPosition);
            m_OriginalElementPosition = m_EdgeModel.EdgeControlPoints.ElementAt(m_ControlPointIndex).Position;
            m_OriginalTightness = m_EdgeModel.EdgeControlPoints.ElementAt(m_ControlPointIndex).Tightness;

            if (e.modifiers == EventModifiers.None)
            {
                m_DraggingControlPoint = true;
            }
            else if (e.modifiers == EventModifiers.Alt)
            {
                m_DraggingTightness = true;
            }

            if (m_DraggingControlPoint || m_DraggingTightness)
            {
                this.CapturePointer(e.pointerId);
                e.StopPropagation();
            }
        }

        void OnPointerMove(PointerMoveEvent e)
        {
            GraphView graphView = null;
            Vector2 pointerDelta = Vector2.zero;
            if (m_DraggingControlPoint || m_DraggingTightness)
            {
                graphView = GetFirstAncestorOfType<GraphView>();
                var pointerPosition = this.ChangeCoordinatesTo(parent, e.localPosition);
                pointerDelta = new Vector2(pointerPosition.x, pointerPosition.y) - m_OriginalPointerPosition;
            }

            if (graphView == null)
            {
                return;
            }

            if (m_DraggingControlPoint)
            {
                var newPosition = m_OriginalElementPosition + pointerDelta;
                graphView.Store.Dispatch(new MoveEdgeControlPointAction(m_EdgeModel, m_ControlPointIndex, newPosition, m_OriginalTightness));

                // PF FIXME: action should mark edge model as dirty
                using (var evt = GeometryChangedEvent.GetPooled(m_EdgeControl.layout, m_EdgeControl.layout))
                {
                    evt.target = m_EdgeControl;
                    SendEvent(evt);
                }

                e.StopPropagation();
            }
            else if (m_DraggingTightness)
            {
                var tightnessDelta = pointerDelta.x - pointerDelta.y;
                var newTightness = m_OriginalTightness + tightnessDelta;
                graphView.Store.Dispatch(new MoveEdgeControlPointAction(m_EdgeModel, m_ControlPointIndex, m_OriginalElementPosition, newTightness));

                // PF FIXME: action should mark edge model as dirty
                using (var evt = GeometryChangedEvent.GetPooled(m_EdgeControl.layout, m_EdgeControl.layout))
                {
                    evt.target = m_EdgeControl;
                    SendEvent(evt);
                }

                e.StopPropagation();
            }
        }

        void OnPointerUp(PointerUpEvent e)
        {
            if (!e.isPrimary || e.button != 0)
                return;

            this.ReleasePointer(e.pointerId);
            m_DraggingControlPoint = false;
            m_DraggingTightness = false;
            e.StopPropagation();
        }

        public void SetPositions(Vector2 cpPosition, Vector2 lhPosition, Vector2 rhPosition)
        {
            style.left = cpPosition.x;
            style.top = cpPosition.y;
        }

        protected virtual void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.menu.MenuItems().Count > 0)
                evt.menu.AppendSeparator();

            evt.menu.AppendAction(k_RemoveControlPointMenuItem, menuAction =>
            {
                var graphView = GetFirstAncestorOfType<GraphView>();
                if (graphView == null)
                    return;

                int controlPointIndex = parent.Children().IndexOf(this);
                graphView.Store.Dispatch(new RemoveEdgeControlPointAction(m_EdgeModel, controlPointIndex));
            });
        }
    }
}
