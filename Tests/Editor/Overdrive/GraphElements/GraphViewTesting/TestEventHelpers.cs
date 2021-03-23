//#define ENABLE_EVENTHELPER_TRACE

using System;
using System.Text.RegularExpressions;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    public class TestEventHelpers
    {
        readonly EditorWindow m_Window;

        public TestEventHelpers(EditorWindow window)
        {
            m_Window = window;
        }

        public const EventModifiers multiSelectModifier =
#if UNITY_EDITOR_OSX
            EventModifiers.Command;
#else
            EventModifiers.Control;
#endif

        //-----------------------------------------------------------
        // MouseDown Event Helpers
        //-----------------------------------------------------------
        public void MouseDownEvent(Vector2 point, int count, MouseButton mouseButton = MouseButton.LeftMouse, EventModifiers eventModifiers = EventModifiers.None)
        {
#if ENABLE_EVENTHELPER_TRACE
            Debug.Log("MouseDown: [" + eventModifiers + "][" + mouseButton + "] @" + point);
#endif
            m_Window.SendEvent(
                new Event
                {
                    type = EventType.MouseDown,
                    mousePosition = point,
                    clickCount = count,
                    button = (int)mouseButton,
                    modifiers = eventModifiers
                });
        }

        public void MouseDownEvent(Vector2 point, MouseButton mouseButton = MouseButton.LeftMouse,
            EventModifiers eventModifiers = EventModifiers.None)
        {
            MouseDownEvent(point, 1, mouseButton, eventModifiers);
        }

        public void MouseDownEvent(VisualElement element, MouseButton mouseButton = MouseButton.LeftMouse, EventModifiers eventModifiers = EventModifiers.None)
        {
            MouseDownEvent(element.worldBound.center, mouseButton, eventModifiers);
        }

        //-----------------------------------------------------------
        // MouseUp Event Helpers
        //-----------------------------------------------------------
        public void MouseUpEvent(Vector2 point, int count, MouseButton mouseButton = MouseButton.LeftMouse, EventModifiers eventModifiers = EventModifiers.None)
        {
#if ENABLE_EVENTHELPER_TRACE
            Debug.Log("MouseUp: [" + eventModifiers + "][" + mouseButton + "] @" + point);
#endif

            m_Window.SendEvent(
                new Event
                {
                    type = EventType.MouseUp,
                    mousePosition = point,
                    clickCount = count,
                    button = (int)mouseButton,
                    modifiers = eventModifiers
                });
        }

        public void MouseUpEvent(Vector2 point, MouseButton mouseButton = MouseButton.LeftMouse,
            EventModifiers eventModifiers = EventModifiers.None)
        {
            MouseUpEvent(point, 1, mouseButton, eventModifiers);
        }

        public void MouseUpEvent(VisualElement element, MouseButton mouseButton = MouseButton.LeftMouse, EventModifiers eventModifiers = EventModifiers.None)
        {
            MouseUpEvent(element.worldBound.center, mouseButton, eventModifiers);
        }

        //-----------------------------------------------------------
        // MouseDrag Event Helpers
        //-----------------------------------------------------------
        public void MouseDragEvent(Vector2 start, Vector2 end, MouseButton mouseButton = MouseButton.LeftMouse, EventModifiers eventModifiers = EventModifiers.None)
        {
#if ENABLE_EVENTHELPER_TRACE
            Debug.Log("MouseDrag: [" + eventModifiers + "][" + mouseButton + "] @" + start + " -> " + end);
#endif

            m_Window.SendEvent(
                new Event
                {
                    type = EventType.MouseDrag,
                    mousePosition = end,
                    button = (int)mouseButton,
                    delta = end - start,
                    modifiers = eventModifiers
                });
        }

        //-----------------------------------------------------------
        // MouseMove Event Helpers
        //-----------------------------------------------------------
        public void MouseMoveEvent(Vector2 start, Vector2 end, MouseButton mouseButton = MouseButton.LeftMouse, EventModifiers eventModifiers = EventModifiers.None)
        {
#if ENABLE_EVENTHELPER_TRACE
            Debug.Log("MouseMove: [" + eventModifiers + "][" + mouseButton + "] @" + start + " -> " + end);
#endif

            m_Window.SendEvent(
                new Event
                {
                    type = EventType.MouseMove,
                    mousePosition = end,
                    button = (int)mouseButton,
                    delta = end - start,
                    modifiers = eventModifiers
                });
        }

        //-----------------------------------------------------------
        // ScrollWheel Event Helpers
        //-----------------------------------------------------------
        public void ScrollWheelEvent(float scrollDelta, Vector2 mousePosition, EventModifiers eventModifiers = EventModifiers.None)
        {
#if ENABLE_EVENTHELPER_TRACE
            Debug.Log("ScrollWheel: [" + eventModifiers + "] @" + mousePosition + " delta:" + scrollDelta);
#endif

            m_Window.SendEvent(
                new Event
                {
                    type = EventType.ScrollWheel,
                    modifiers = eventModifiers,
                    mousePosition = mousePosition,
                    delta = new Vector2(scrollDelta, scrollDelta)
                });
        }

        //-----------------------------------------------------------
        // Keyboard Event Helpers
        //-----------------------------------------------------------
        public void KeyDownEvent(KeyCode key, EventModifiers eventModifiers = EventModifiers.None)
        {
#if ENABLE_EVENTHELPER_TRACE
            Debug.Log("KeyDown: [" + eventModifiers + "][" + key + "]");
#endif

            // In Unity, key down are sent twice: once with keycode, once with character.

            m_Window.SendEvent(
                new Event
                {
                    type = EventType.KeyDown,
                    keyCode = key,
                    modifiers = eventModifiers
                });

            m_Window.SendEvent(
                new Event
                {
                    type = EventType.KeyDown,
                    character = (char)key,
                    modifiers = eventModifiers
                });
        }

        public void KeyUpEvent(KeyCode key, EventModifiers eventModifiers = EventModifiers.None)
        {
#if ENABLE_EVENTHELPER_TRACE
            Debug.Log("KeyUp: [" + eventModifiers + "][" + key + "]");
#endif

            m_Window.SendEvent(
                new Event
                {
                    type = EventType.KeyUp,
                    character = (char)key,
                    keyCode = key,
                    modifiers = eventModifiers
                });
        }

        //-----------------------------------------------------------
        // Clicking Helpers
        //-----------------------------------------------------------
        public void Click(Vector2 point, MouseButton mouseButton = MouseButton.LeftMouse, EventModifiers eventModifiers = EventModifiers.None, int clickCount = 1)
        {
            MouseDownEvent(point, clickCount, mouseButton, eventModifiers);
            MouseUpEvent(point, clickCount, mouseButton, eventModifiers);
        }

        public void Click(VisualElement element, MouseButton mouseButton = MouseButton.LeftMouse, EventModifiers eventModifiers = EventModifiers.None, int clickCount = 1)
        {
            Click(element.worldBound.center, mouseButton, eventModifiers, clickCount);
        }

        //-----------------------------------------------------------
        // Dragging Helpers
        //-----------------------------------------------------------
        public void DragTo(Vector2 start, Vector2 end, MouseButton mouseButton = MouseButton.LeftMouse, EventModifiers eventModifiers = EventModifiers.None, int steps = 1)
        {
            MouseDownEvent(start, mouseButton, eventModifiers);
            Vector2 increment = (end - start) / steps;
            for (int i = 0; i < steps; i++)
            {
                MouseDragEvent(start + i * increment, start + (i + 1) * increment, mouseButton, eventModifiers);
            }
            MouseUpEvent(end, mouseButton, eventModifiers);
        }

        public void DragToNoRelease(Vector2 start, Vector2 end, MouseButton mouseButton = MouseButton.LeftMouse, EventModifiers eventModifiers = EventModifiers.None, int steps = 1)
        {
            MouseDownEvent(start, mouseButton, eventModifiers);
            Vector2 increment = (end - start) / steps;
            for (int i = 0; i < steps; i++)
            {
                MouseDragEvent(start + i * increment, start + (i + 1) * increment, mouseButton, eventModifiers);
            }
        }

        //-----------------------------------------------------------
        // KeyPressed Helpers
        //-----------------------------------------------------------

        public void KeyPressed(KeyCode key, EventModifiers eventModifiers = EventModifiers.None)
        {
            KeyDownEvent(key, eventModifiers);
            KeyUpEvent(key, eventModifiers);
        }

        public void Type(string text)
        {
            foreach (var c in text)
            {
                KeyPressed((KeyCode)c);
            }
        }

        //-----------------------------------------------------------
        // ValidateCommand Helpers
        //-----------------------------------------------------------
        public bool ValidateCommand(string command)
        {
#if ENABLE_EVENTHELPER_TRACE
            Debug.Log("ValidateCommand: [" + command + "]");
#endif

            return m_Window.SendEvent(new Event { type = EventType.ValidateCommand, commandName = command });
        }

        //-----------------------------------------------------------
        // ExecuteCommand Helpers
        //-----------------------------------------------------------
        public bool ExecuteCommand(string command)
        {
#if ENABLE_EVENTHELPER_TRACE
            Debug.Log("ExecuteCommand: [" + command + "]");
#endif

            return m_Window.SendEvent(new Event { type = EventType.ExecuteCommand, commandName = command });
        }
    }
}
