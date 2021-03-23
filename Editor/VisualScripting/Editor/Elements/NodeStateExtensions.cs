using System;
using UnityEngine.UIElements;

namespace UnityEditor.VisualScripting.Editor
{
    static class NodeStateExtensions
    {
        public static void ApplyNodeState(this INodeState node)
        {
            ((VisualElement)node).EnableInClassList(NodeUIState.Unused.ToString(), node.UIState == NodeUIState.Unused);
            ((VisualElement)node).EnableInClassList(NodeUIState.Disabled.ToString(), node.UIState == NodeUIState.Disabled);
        }

        public static void AddOverlay(this INodeState node)
        {
            var disabledOverlay = new VisualElement { name = "disabledOverlay", pickingMode = PickingMode.Ignore };
            ((VisualElement)node).hierarchy.Add(disabledOverlay);
        }
    }
}
