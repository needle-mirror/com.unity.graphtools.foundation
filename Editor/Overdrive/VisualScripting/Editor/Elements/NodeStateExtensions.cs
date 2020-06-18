using System;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
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
            var disabledOverlay = new VisualElement {name = "disabledOverlay", pickingMode = PickingMode.Ignore};
            ((VisualElement)node).hierarchy.Add(disabledOverlay);
        }

        public static readonly string k_UssClassName = "ge-selection-border";
        public static readonly string k_SelectionBorderElementName = "selection-border";
        public static readonly string k_ContentContainerElementName = "content-container";

        public static VisualElement AddBorder(this VisualElement element, string elementClassName)
        {
            // [VSB-695]
            // Setting pickingMode to Ignore so that our clumsy drag and drop implementation work for placemats.
            // Dnd is handled by the graph view and it looks at the event target. This means the event target
            // should be the placemat (in general, the element having the border), not the border or the content container.
            // This would be unneeded if the placemat handled DnD itself.
            var selectionBorder = new VisualElement { name = k_SelectionBorderElementName, pickingMode = PickingMode.Ignore  };
            selectionBorder.AddToClassList(k_UssClassName);
            selectionBorder.AddToClassList(elementClassName.WithUssElement(k_SelectionBorderElementName));
            element.Add(selectionBorder);

            var contentContainerElement = new VisualElement { name = k_ContentContainerElementName, pickingMode = PickingMode.Ignore };
            selectionBorder.AddToClassList(k_UssClassName.WithUssElement(k_ContentContainerElementName));
            contentContainerElement.AddToClassList(elementClassName.WithUssElement(k_ContentContainerElementName));
            selectionBorder.Add(contentContainerElement);

            element.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(UICreationHelper.templatePath + "SelectionBorder.uss"));

            return contentContainerElement;
        }
    }
}
