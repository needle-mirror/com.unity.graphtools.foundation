using System;

namespace UnityEditor.VisualScripting.Editor
{
    interface INodeState : IHasGraphElementModel
    {
        NodeUIState UIState { get; set; }
    }

    public enum NodeUIState
    {
        Enabled,
        Disabled,
        Unused,
    }

    public enum ModelState
    {
        Enabled = 0, // default value
        Disabled,
    }
}
