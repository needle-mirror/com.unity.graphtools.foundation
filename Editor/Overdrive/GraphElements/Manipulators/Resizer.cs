using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class Resizer : VisualElement
    {
        public static readonly string ussClassName = "ge-resizer";
        public static readonly string bothDirectionModifierClassName = ussClassName.WithUssModifier("both-directions");
        public static readonly string horizontalModifierClassName = ussClassName.WithUssModifier("horizontal");
        public static readonly string verticalModifierClassName = ussClassName.WithUssModifier("vertical");
        public static readonly string iconElementUssClassName = ussClassName.WithUssElement("icon");

        Vector2 m_Start;
        Vector2 m_MinimumSize;
        Rect m_StartPos;
        Action m_OnResizedCallback;
        static readonly Vector2 k_ResizerSize = new Vector2(30.0f, 30.0f);

        public MouseButton ActivateButton { get; set; }

        bool m_Active;

        public Resizer() :
            this(k_ResizerSize)
        {
        }

        public Resizer(Vector2 minimumSize, Action onResizedCallback = null)
        {
            m_MinimumSize = minimumSize;
            m_Active = false;
            m_OnResizedCallback = onResizedCallback;

            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<MouseUpEvent>(OnMouseUp);
            RegisterCallback<MouseMoveEvent>(OnMouseMove);

            ClearClassList();
            AddToClassList(ussClassName);

            var icon = new VisualElement();
            icon.AddToClassList(iconElementUssClassName);
            Add(icon);
        }

        void OnMouseDown(MouseDownEvent e)
        {
            if (m_Active)
            {
                e.StopImmediatePropagation();
                return;
            }

            var targetPanel = (e.target as VisualElement)?.panel;
            if (targetPanel.GetCapturingElement(PointerId.mousePointerId) != null)
                return;

            if (!(parent is GraphElement ce))
                return;

            if (!ce.IsResizable())
                return;

            if (e.button == (int)ActivateButton)
            {
                m_Start = this.ChangeCoordinatesTo(parent, e.localMousePosition);
                m_StartPos = parent.layout;
                // Warn user if target uses a relative CSS position type
                if (!parent.IsLayoutManual() && parent.resolvedStyle.position == Position.Relative)
                {
                    Debug.LogWarning("Attempting to resize an object with a non absolute position");
                }

                m_Active = true;
                this.CaptureMouse();
                e.StopPropagation();
            }
        }

        void OnMouseUp(MouseUpEvent e)
        {
            var ce = parent as GraphElement;
            if (ce == null)
                return;

            if (!ce.IsResizable())
                return;

            if (!m_Active)
                return;

            if (e.button == (int)ActivateButton && m_Active)
            {
                if (m_OnResizedCallback != null)
                    m_OnResizedCallback();

                m_Active = false;
                this.ReleaseMouse();
                e.StopPropagation();
            }
        }

        void OnMouseMove(MouseMoveEvent e)
        {
            var ce = parent as GraphElement;
            if (ce == null)
                return;

            if (!ce.IsResizable())
                return;

            // Then can be resize in all direction
            if (ce.ResizeRestriction == ResizeRestriction.None)
            {
                if (ClassListContains(bothDirectionModifierClassName) == false)
                {
                    AddToClassList(bothDirectionModifierClassName);
                    RemoveFromClassList(horizontalModifierClassName);
                    RemoveFromClassList(verticalModifierClassName);
                }
            }
            else if (ce.resolvedStyle.position == Position.Absolute)
            {
                if (ce.resolvedStyle.flexDirection == FlexDirection.Column)
                {
                    if (ClassListContains(horizontalModifierClassName) == false)
                    {
                        AddToClassList(horizontalModifierClassName);
                        RemoveFromClassList(bothDirectionModifierClassName);
                        RemoveFromClassList(verticalModifierClassName);
                    }
                }
                else if (ce.resolvedStyle.flexDirection == FlexDirection.Row)
                {
                    if (ClassListContains(verticalModifierClassName) == false)
                    {
                        AddToClassList(verticalModifierClassName);
                        RemoveFromClassList(bothDirectionModifierClassName);
                        RemoveFromClassList(horizontalModifierClassName);
                    }
                }
            }

            if (m_Active)
            {
                Vector2 diff = this.ChangeCoordinatesTo(parent, e.localMousePosition) - m_Start;
                var newSize = new Vector2(m_StartPos.width + diff.x, m_StartPos.height + diff.y);
                float minWidth = ce.resolvedStyle.minWidth == StyleKeyword.Auto ? 0 : ce.resolvedStyle.minWidth.value;
                minWidth = Math.Max(minWidth, m_MinimumSize.x);
                float minHeight = ce.resolvedStyle.minHeight == StyleKeyword.Auto ? 0 : ce.resolvedStyle.minHeight.value;
                minHeight = Math.Max(minHeight, m_MinimumSize.y);
                float maxWidth = ce.resolvedStyle.maxWidth == StyleKeyword.None ? float.MaxValue : ce.resolvedStyle.maxWidth.value;
                float maxHeight = ce.resolvedStyle.maxHeight == StyleKeyword.None ? float.MaxValue : ce.resolvedStyle.maxHeight.value;

                newSize.x = (newSize.x < minWidth) ? minWidth : ((newSize.x > maxWidth) ? maxWidth : newSize.x);
                newSize.y = (newSize.y < minHeight) ? minHeight : ((newSize.y > maxHeight) ? maxHeight : newSize.y);

                if (ce.GetPosition().size != newSize)
                {
                    if (ce.IsLayoutManual())
                    {
                        ce.SetPosition(new Rect(ce.layout.x, ce.layout.y, newSize.x, newSize.y));
                    }
                    else if (ce.ResizeRestriction == ResizeRestriction.None)
                    {
                        ce.style.width = newSize.x;
                        ce.style.height = newSize.y;
                    }
                    else if (parent.resolvedStyle.flexDirection == FlexDirection.Column)
                    {
                        ce.style.width = newSize.x;
                    }
                    else if (parent.resolvedStyle.flexDirection == FlexDirection.Row)
                    {
                        ce.style.height = newSize.y;
                    }
                }

                e.StopPropagation();
            }
        }
    }
}
