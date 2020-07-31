using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    partial class VseMenu
    {
        public Action<ChangeEvent<bool>> OnToggleTracing;

        ToolbarToggle m_EnableTracingButton;

        void CreateTracingMenu()
        {
            m_EnableTracingButton = this.MandatoryQ<ToolbarToggle>("enableTracingButton");
            m_EnableTracingButton.tooltip = "Toggle Tracing For Current Instance";
            m_EnableTracingButton.SetValueWithoutNotify(m_Store.GetState().EditorDataModel.TracingEnabled);
            m_EnableTracingButton.RegisterValueChangedCallback(e => OnToggleTracing?.Invoke(e));
        }
    }
}
