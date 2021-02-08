using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public enum ShortcutHandled
    {
        NotHandled,
        Handled
    }

    public delegate ShortcutHandled ShortcutDelegate(KeyDownEvent evt);

    public class ShortcutHandler : Manipulator
    {
        readonly Dictionary<Event, ShortcutDelegate> m_Dictionary;

        public ShortcutHandler(Dictionary<Event, ShortcutDelegate> dictionary)
        {
            m_Dictionary = dictionary;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<KeyDownEvent>(OnKeyDown);
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            var panel = (evt.target as VisualElement)?.panel;
            if (panel.GetCapturingElement(PointerId.mousePointerId) != null)
                return;

            if (m_Dictionary.ContainsKey(evt.imguiEvent))
            {
                var handled = m_Dictionary[evt.imguiEvent](evt);
                if (handled == ShortcutHandled.Handled)
                {
                    evt.StopPropagation();
                    evt.imguiEvent?.Use();
                }
            }
        }
    }
}
