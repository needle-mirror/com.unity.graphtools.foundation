using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class SelectionBorder : VisualElement
    {
        public static readonly string k_UssClassName = "ge-selection-border";
        public static readonly string k_ContentContainerElementName = "content-container";

        public VisualElement ContentContainer { get; }

        public SelectionBorder()
        {
            // [VSB-695]
            // Setting pickingMode to Ignore so that our clumsy drag and drop implementation work for placemats.
            // Dnd is handled by the graph view and it looks at the event target. This means the event target
            // should be the placemat (in general, the element having the border), not the border or the content container.
            // This would be unneeded if the placemat handled DnD itself.
            pickingMode = PickingMode.Ignore;
            AddToClassList(k_UssClassName);

            ContentContainer = new VisualElement { name = k_ContentContainerElementName, pickingMode = PickingMode.Ignore };
            ContentContainer.AddToClassList(k_UssClassName.WithUssElement(k_ContentContainerElementName));
            Add(ContentContainer);

            this.AddStylesheet("SelectionBorder.uss");
        }
    }
}
