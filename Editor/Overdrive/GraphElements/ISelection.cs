using System.Collections.Generic;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    public interface ISelection
    {
        List<ISelectableGraphElement> Selection { get; }

        void AddToSelection(ISelectableGraphElement selectable);
        void RemoveFromSelection(ISelectableGraphElement selectable);
        void ClearSelection();
    }
}
