using System;

namespace UnityEngine.GraphToolsFoundation.CommandStateObserver
{
    /// <summary>
    /// Class used for bidirectional communication with the <see cref="IUndo"/>.
    /// </summary>
    public static class Undo
    {
        internal delegate void UndoRedoCallback();

        internal static UndoRedoCallback undoRedoPerformed;

        /// <summary>
        /// The undo proxy object.
        /// </summary>
        public static IUndo UndoProxy { private get; set; }

        /// <summary>
        /// Notifies that an undo/redo operation was performed.
        /// </summary>
        public static void NotifyUndoRedoPerformed()
        {
            undoRedoPerformed?.Invoke();
        }

        internal static void IncrementCurrentGroup()
        {
            UndoProxy?.IncrementCurrentGroup();
        }

        internal static void SetCurrentGroupName(string name)
        {
            UndoProxy?.SetCurrentGroupName(name);
        }

        internal static void RegisterCompleteObjectUndo(Object[] objects, string undoString)
        {
            UndoProxy?.RegisterCompleteObjectUndo(objects, undoString);
        }
    }
}
