using System;

namespace UnityEngine.UIElements
{
    public class SimpleClickable : MouseManipulator
    {
        public SimpleClickable(Action<MouseDownEvent> handler)
        {
            // ISSUE: reference to a compiler-generated field
            ClickedWithEventInfo = handler;
            activators.Add(new ManipulatorActivationFilter
            {
                button = MouseButton.LeftMouse
            });
        }

        public event Action<MouseDownEvent> ClickedWithEventInfo;

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
        }

        void OnMouseDown(MouseDownEvent evt)
        {
            if (!CanStartManipulation(evt))
                return;
            ClickedWithEventInfo?.Invoke(evt);
        }
    }
}
