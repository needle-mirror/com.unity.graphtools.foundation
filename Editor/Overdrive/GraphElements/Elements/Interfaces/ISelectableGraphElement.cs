using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface ISelectableGraphElement : IGraphElement
    {
        bool IsSelectable();
        void Select(VisualElement selectionContainer, bool additive);
        void Unselect(VisualElement selectionContainer);
        bool IsSelected(VisualElement selectionContainer);
    }
}
