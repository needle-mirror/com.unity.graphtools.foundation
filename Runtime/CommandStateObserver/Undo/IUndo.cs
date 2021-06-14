using System;

namespace UnityEngine.GraphToolsFoundation.CommandStateObserver
{
    /// <summary>
    /// Interface for classes providing undo data storage.
    /// </summary>
    public interface IUndo
    {
        /// <summary>
        /// Increments the current undo group. See <see cref="UnityEditor.Undo.IncrementCurrentGroup"/>.
        /// </summary>
        public void IncrementCurrentGroup();

        /// <summary>
        /// Sets the current undo group name. See <see cref="UnityEditor.Undo.SetCurrentGroupName"/>.
        /// </summary>
        /// <param name="name">The group name.</param>
        public void SetCurrentGroupName(string name);

        /// <summary>
        /// Adds the <paramref name="objects"/> on the undo stack.
        /// See <see cref="UnityEditor.Undo.RegisterCompleteObjectUndo(Object[], string)"/>.
        /// </summary>
        /// <param name="objects">The objects to add on the undo stack.</param>
        /// <param name="undoString">The name of the undoable operation.</param>
        public void RegisterCompleteObjectUndo(Object[] objects, string undoString);
    }
}
