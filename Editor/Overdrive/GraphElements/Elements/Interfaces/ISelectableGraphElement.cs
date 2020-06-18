using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    public interface ISelectableGraphElement
    {
        bool IsSelectable();
        void Select(VisualElement selectionContainer, bool additive);
        void Unselect(VisualElement selectionContainer);
        bool IsSelected(VisualElement selectionContainer);
    }
}
