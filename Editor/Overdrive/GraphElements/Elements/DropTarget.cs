using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// IDropTarget ModelUI helper class
    /// Has callbacks for all the Drag And Drop events
    ///
    /// See Port for an example of derivation
    /// </summary>
    public abstract class DropTarget : ModelUI
    {
        public abstract bool CanAcceptSelectionDrop(IReadOnlyList<ISelectableGraphElement> selection);

        /// <summary>
        /// Always called whenever drop gets done, canceled or exited
        /// Useful to remove custom style when element isn't targeted by a drop anymore
        /// </summary>
        protected virtual void OnDragEnd()
        {
        }

        public DropTarget()
        {
            RegisterCallback<DragEnterEvent>(OnDragEnter);
            RegisterCallback<DragLeaveEvent>(OnDragLeave);
            RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
            RegisterCallback<DragPerformEvent>(OnDragPerform);
            RegisterCallback<DragExitedEvent>(OnDragExited);
        }

        /// <summary>
        /// True if their is a drag and drop in progress and we handle it
        /// Allows not calling CanAcceptDrop several times during the same drag and drop operation
        /// </summary>
        protected bool CurrentDropAccepted { get; private set; }


        /// <summary>
        /// Handler for any DragEnterEvent passed to this element
        /// </summary>
        /// <param name="evt">event passed</param>
        public virtual void OnDragEnter(DragEnterEvent evt)
        {
            CurrentDropAccepted = CanAcceptSelectionDrop(GraphView.Selection);
        }

        /// <summary>
        /// Handler for any DragLeaveEvent passed to this element
        /// </summary>
        /// <param name="evt">event passed</param>
        public virtual void OnDragLeave(DragLeaveEvent evt)
        {
            OnDragEnd();
        }

        /// <summary>
        /// Handler for any DragUpdatedEvent passed to this element
        /// </summary>
        /// <param name="evt">event passed</param>
        public virtual void OnDragUpdated(DragUpdatedEvent evt)
        {
            DragAndDrop.visualMode = CurrentDropAccepted ? DragAndDropVisualMode.Link : DragAndDropVisualMode.Rejected;
        }

        /// <summary>
        /// Handler for any DragPerformEvent passed to this element
        /// </summary>
        /// <param name="evt">event passed</param>
        public virtual void OnDragPerform(DragPerformEvent evt)
        {
            OnDragEnd();
        }

        /// <summary>
        /// Handler for any DragExitedEvent passed to this element
        /// </summary>
        /// <param name="evt">event passed</param>
        public virtual void OnDragExited(DragExitedEvent evt)
        {
            OnDragEnd();
        }
    }
}
