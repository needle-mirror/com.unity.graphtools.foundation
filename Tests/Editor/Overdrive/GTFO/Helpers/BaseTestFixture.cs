using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO
{
    public class BaseTestFixture
    {
        protected TestEditorWindow m_Window;

        [SetUp]
        public void SetUp()
        {
            m_Window = EditorWindow.GetWindow<TestEditorWindow>();
        }

        [TearDown]
        public void TearDown()
        {
            m_Window.Close();
        }

        protected static void Click(VisualElement target, Vector2 mousePosition)
        {
            using (var e = MouseDownEvent.GetPooled(mousePosition, 0, 1, Vector2.zero))
            {
                e.target = target;
                target.SendEvent(e);
            }
            using (var e = MouseUpEvent.GetPooled(mousePosition, 0, 1, Vector2.zero))
            {
                e.target = target;
                target.SendEvent(e);
            }
        }

        protected static void DoubleClick(VisualElement target, Vector2 worldMousePosition)
        {
            using (var e = MouseDownEvent.GetPooled(worldMousePosition, 0, 2, Vector2.zero))
            {
                e.target = target;
                target.SendEvent(e);
            }
            using (var e = MouseUpEvent.GetPooled(worldMousePosition, 0, 2, Vector2.zero))
            {
                e.target = target;
                target.SendEvent(e);
            }
        }

        protected static void ClickDragNoRelease(VisualElement target, Vector2 mousePosition, Vector2 moveDelta)
        {
            using (var e = MouseDownEvent.GetPooled(mousePosition, 0, 1, Vector2.zero))
            {
                e.target = target;
                target.SendEvent(e);
            }
            using (var e = MouseMoveEvent.GetPooled(mousePosition + moveDelta, 0, 1, Vector2.zero))
            {
                e.target = target;
                target.SendEvent(e);
            }
        }

        protected static void ReleaseMouse(VisualElement target, Vector2 mousePosition)
        {
            using (var e = MouseUpEvent.GetPooled(mousePosition, 0, 1, Vector2.zero))
            {
                e.target = target;
                target.SendEvent(e);
            }
        }

        protected static void ClickDragRelease(VisualElement target, Vector2 mousePosition, Vector2 moveDelta)
        {
            ClickDragNoRelease(target, mousePosition, moveDelta);
            ReleaseMouse(target, mousePosition + moveDelta);
        }

        static KeyCode GetKeyCode(char c)
        {
            return (KeyCode)char.ToLower(c);
        }

        protected static void Type(VisualElement target, char c)
        {
            Event kde1 = new Event
            {
                type = EventType.KeyDown,
                character = c,
                keyCode = KeyCode.None,
                modifiers = EventModifiers.None
            };
            using (var e = KeyDownEvent.GetPooled(kde1))
            {
                e.target = null;
                target.SendEvent(e);
            }

            Event kde2 = new Event
            {
                type = EventType.KeyDown,
                character = (char)0,
                keyCode = GetKeyCode(c),
                modifiers = EventModifiers.None
            };
            using (var e = KeyDownEvent.GetPooled(kde2))
            {
                e.target = null;
                target.SendEvent(e);
            }

            Event kue = new Event
            {
                type = EventType.KeyUp,
                character = c,
                keyCode = GetKeyCode(c),
                modifiers = EventModifiers.None
            };
            using (var e = KeyUpEvent.GetPooled(kue))
            {
                e.target = null;
                target.SendEvent(e);
            }
        }

        protected static void Type(VisualElement target, string text)
        {
            foreach (var c in text)
            {
                Type(target, c);
            }
        }

        protected static void Type(VisualElement target, KeyCode key)
        {
            Type(target, (char)key);
        }
    }
}
