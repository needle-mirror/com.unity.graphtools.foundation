using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Bridge
{
    public class PromptSearcherEvent : EventBase<PromptSearcherEvent>
    {
        public Vector2 MenuPosition;

        public static PromptSearcherEvent GetPooled(Vector2 menuPosition)
        {
            var e = GetPooled();
            e.MenuPosition = menuPosition;
            return e;
        }

        protected override void Init()
        {
            base.Init();
            propagation = EventPropagation.TricklesDown | EventPropagation.Bubbles | EventPropagation.Cancellable;
        }
    }
}
