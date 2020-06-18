using System.Collections.Generic;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    public interface ISelection
    {
        List<ISelectableGraphElement> selection { get; }

        void AddToSelection(ISelectableGraphElement selectable);
        void RemoveFromSelection(ISelectableGraphElement selectable);
        void ClearSelection();
    }
}
