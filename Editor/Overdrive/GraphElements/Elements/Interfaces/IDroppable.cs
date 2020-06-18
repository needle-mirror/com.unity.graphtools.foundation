using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    public interface IDroppable
    {
        bool IsDroppable();
    }

    public interface IDropTarget
    {
        bool CanAcceptDrop(List<ISelectableGraphElement> selection);

        // evt.mousePosition will be in global coordinates.
        bool DragUpdated(DragUpdatedEvent evt, IEnumerable<ISelectableGraphElement> selection, IDropTarget dropTarget, ISelection dragSource);
        bool DragPerform(DragPerformEvent evt, IEnumerable<ISelectableGraphElement> selection, IDropTarget dropTarget, ISelection dragSource);
        bool DragEnter(DragEnterEvent evt, IEnumerable<ISelectableGraphElement> selection, IDropTarget enteredTarget, ISelection dragSource);
        bool DragLeave(DragLeaveEvent evt, IEnumerable<ISelectableGraphElement> selection, IDropTarget leftTarget, ISelection dragSource);
        bool DragExited();
    }
}
