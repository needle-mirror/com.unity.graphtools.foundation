using System;
using System.Linq;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Displays a control point of an <see cref="Edge"/>.
    /// </summary>
    public class EdgeControlPoint : VisualElement
    {
        public static readonly string ussClassName = "ge-edge-control-point";

        public static readonly string removeControlPointMenuItem = "Remove control point";

        protected EdgeControl m_EdgeControl;

        protected IEditableEdge m_EdgeModel;

        protected int m_ControlPointIndex;

        bool m_DraggingControlPoint;

        bool m_DraggingTightness;

        Vector2 m_OriginalElementPosition;

        float m_OriginalTightness;

        Vector2 m_OriginalPointerPosition;

        ContextualMenuManipulator m_ContextualMenuManipulator;

        /// <summary>
        /// The contextual menu manipulator.
        /// </summary>
        protected ContextualMenuManipulator ContextualMenuManipulator
        {
            get => m_ContextualMenuManipulator;
            set => this.ReplaceManipulator(ref m_ContextualMenuManipulator, value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EdgeControlPoint"/> class.
        /// </summary>
        /// <param name="edgeControl">The associated <see cref="EdgeControl"/>.</param>
        /// <param name="edgeModel">The edge model.</param>
        /// <param name="controlPointIndex">The index of this control point.</param>
        public EdgeControlPoint(EdgeControl edgeControl, IEditableEdge edgeModel, int controlPointIndex)
        {
            m_EdgeControl = edgeControl;
            m_EdgeModel = edgeModel;
            m_ControlPointIndex = controlPointIndex;

            AddToClassList(ussClassName);

            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);

            style.position = Position.Absolute;

            ContextualMenuManipulator = new ContextualMenuManipulator(BuildContextualMenu);
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
                graphView = m_EdgeControl.GraphView;
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
                graphView.CommandDispatcher.Dispatch(new MoveEdgeControlPointCommand(m_EdgeModel, m_ControlPointIndex, newPosition, m_OriginalTightness));

                // PF FIXME: command should mark edge model as dirty
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
                graphView.CommandDispatcher.Dispatch(new MoveEdgeControlPointCommand(m_EdgeModel, m_ControlPointIndex, m_OriginalElementPosition, newTightness));

                // PF FIXME: command should mark edge model as dirty
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

        /// <summary>
        /// Sets the position of a control point.
        /// </summary>
        /// <param name="cpPosition">The new position of the control point.</param>
        public void SetPositions(Vector2 cpPosition)
        {
            style.left = cpPosition.x;
            style.top = cpPosition.y;
        }

        /// <summary>
        /// Adds menu elements to the contextual menu.
        /// </summary>
        /// <param name="evt">The event that triggered the construction of the contextual menu.</param>
        protected virtual void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.menu.MenuItems().Count > 0)
                evt.menu.AppendSeparator();

            evt.menu.AppendAction(removeControlPointMenuItem, menuAction =>
            {
                var graphView = GetFirstAncestorOfType<GraphView>();
                if (graphView == null)
                    return;

                int controlPointIndex = parent.Children().IndexOfInternal(this);
                graphView.CommandDispatcher.Dispatch(new RemoveEdgeControlPointCommand(m_EdgeModel, controlPointIndex));
            });
        }
    }
}
