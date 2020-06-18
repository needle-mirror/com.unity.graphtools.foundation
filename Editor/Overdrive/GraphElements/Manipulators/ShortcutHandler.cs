using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    public enum EventPropagation
    {
        Stop,
        Continue
    }

    public delegate EventPropagation ShortcutDelegate();

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
            IPanel panel = (evt.target as VisualElement)?.panel;
            if (panel.GetCapturingElement(PointerId.mousePointerId) != null)
                return;

            if (m_Dictionary.ContainsKey(evt.imguiEvent))
            {
                var result = m_Dictionary[evt.imguiEvent]();
                if (result == EventPropagation.Stop)
                {
                    evt.StopPropagation();
                    if (evt.imguiEvent != null)
                    {
                        evt.imguiEvent.Use();
                    }
                }
            }
        }
    }
}
