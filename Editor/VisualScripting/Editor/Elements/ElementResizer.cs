using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VisualScripting.Editor
{
    class ElementResizer : MouseManipulator
    {
        readonly ResizableElement.Resizer m_Direction;
        readonly VisualElement m_ResizedElement;

        Vector2 m_StartMouse;
        Vector2 m_StartSize;

        Vector2 m_MinSize;
        Vector2 m_MaxSize;

        Vector2 m_StartPosition;

        bool m_DragStarted;
        bool m_Active;

        public ElementResizer(VisualElement resizedElement, ResizableElement.Resizer direction)
        {
            m_Direction = direction;
            m_ResizedElement = resizedElement;

            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
        }

        static readonly Vector2 k_DefaultMinSize = new Vector2(Mathf.NegativeInfinity, Mathf.NegativeInfinity);
        static readonly Vector2 k_DefaultMaxSize = new Vector2(Mathf.Infinity, Mathf.Infinity);

        readonly CustomStyleProperty<float> m_MinSizeX = new CustomStyleProperty<float>("--unity-resizer-min-x");
        readonly CustomStyleProperty<float> m_MinSizeY = new CustomStyleProperty<float>("--unity-resizer-min-y");
        readonly CustomStyleProperty<float> m_MaxSizeX = new CustomStyleProperty<float>("--unity-resizer-max-x");
        readonly CustomStyleProperty<float> m_MaxSizeY = new CustomStyleProperty<float>("--unity-resizer-max-y");

        void OnMouseDown(MouseDownEvent e)
        {
            if (m_Active)
            {
                e.StopImmediatePropagation();
                return;
            }

            if (!CanStartManipulation(e))
                return;

            VisualElement resizedTarget = m_ResizedElement.parent;
            VisualElement resizedBase = resizedTarget?.parent;
            if (resizedBase == null)
                return;

            m_StartMouse = resizedBase.WorldToLocal(e.mousePosition);
            m_StartSize = new Vector2(resizedTarget.resolvedStyle.width, resizedTarget.resolvedStyle.height);
            m_StartPosition = new Vector2(resizedTarget.resolvedStyle.left, resizedTarget.resolvedStyle.top);

            m_MinSize = resizedTarget.customStyle.TryGetValue(m_MinSizeX, out var minSizeX)  &&
                resizedTarget.customStyle.TryGetValue(m_MinSizeY, out var minSizeY) ? new Vector2(minSizeX, minSizeY) : k_DefaultMinSize;
            m_MaxSize = resizedTarget.customStyle.TryGetValue(m_MaxSizeX, out _) &&
                resizedTarget.customStyle.TryGetValue(m_MaxSizeY, out var maxSizeY) ? new Vector2(minSizeX, maxSizeY) : k_DefaultMaxSize;

            m_DragStarted = false;

            m_Active = true;
            target.CaptureMouse();
            e.StopImmediatePropagation();
        }

        void OnMouseMove(MouseMoveEvent e)
        {
            if (!m_Active)
                return;

            VisualElement resizedTarget = m_ResizedElement.parent;
            VisualElement resizedBase = resizedTarget.parent;
            Vector2 mousePos = resizedBase.WorldToLocal(e.mousePosition);
            if (!m_DragStarted)
            {
                if (resizedTarget is IResizable resizable)
                    resizable.OnStartResize();
                else
                    Debug.LogWarning($"{resizedTarget} should be resizable");
                m_DragStarted = true;
            }

            if ((m_Direction & ResizableElement.Resizer.Right) != 0)
            {
                resizedTarget.style.width = Mathf.Min(m_MaxSize.x, Mathf.Max(m_MinSize.x, m_StartSize.x + mousePos.x - m_StartMouse.x));
            }
            else if ((m_Direction & ResizableElement.Resizer.Left) != 0)
            {
                float delta = mousePos.x - m_StartMouse.x;

                if (m_StartSize.x - delta < m_MinSize.x)
                {
                    delta = -m_MinSize.x + m_StartSize.x;
                }
                else if (m_StartSize.x - delta > m_MaxSize.x)
                {
                    delta = -m_MaxSize.x + m_StartSize.x;
                }

                resizedTarget.style.left = delta + m_StartPosition.x;
                resizedTarget.style.width = -delta + m_StartSize.x;
            }
            if ((m_Direction & ResizableElement.Resizer.Bottom) != 0)
            {
                resizedTarget.style.height = Mathf.Min(m_MaxSize.y, Mathf.Max(m_MinSize.y, m_StartSize.y + mousePos.y - m_StartMouse.y));
            }
            else if ((m_Direction & ResizableElement.Resizer.Top) != 0)
            {
                float delta = mousePos.y - m_StartMouse.y;

                if (m_StartSize.y - delta < m_MinSize.y)
                {
                    delta = -m_MinSize.y + m_StartSize.y;
                }
                else if (m_StartSize.y - delta > m_MaxSize.y)
                {
                    delta = -m_MaxSize.y + m_StartSize.y;
                }
                resizedTarget.style.top = delta + m_StartPosition.y;
                resizedTarget.style.height = -delta + m_StartSize.y;
            }

            e.StopPropagation();
        }

        void OnMouseUp(MouseUpEvent e)
        {
            if (!m_Active || !CanStopManipulation(e))
                return;

            VisualElement resizedTarget = m_ResizedElement.parent;
            if (Math.Abs(resizedTarget.resolvedStyle.width - m_StartSize.x) > float.Epsilon ||
                Math.Abs(resizedTarget.resolvedStyle.height - m_StartSize.y) > float.Epsilon)
            {
                if (resizedTarget is IResizable resizable)
                    resizable.OnResized();
                else
                    Debug.LogWarning($"{resizedTarget} should be resizable");
            }

            m_Active = false;
            target.ReleaseMouse();
            e.StopPropagation();
        }
    }
}
