using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    class UnityEditorUndoProxy : IUndo
    {
        [InitializeOnLoadMethod]
        static void Setup()
        {
            UnityEngine.GraphToolsFoundation.CommandStateObserver.Undo.UndoProxy = new UnityEditorUndoProxy();
            Undo.undoRedoPerformed += UnityEngine.GraphToolsFoundation.CommandStateObserver.Undo.NotifyUndoRedoPerformed;
        }

        /// <inheritdoc />
        public void IncrementCurrentGroup()
        {
            Undo.IncrementCurrentGroup();
        }

        /// <inheritdoc />
        public void SetCurrentGroupName(string name)
        {
            Undo.SetCurrentGroupName(name);
        }

        /// <inheritdoc />
        public void RegisterCompleteObjectUndo(Object[] objects, string undoString)
        {
            Undo.RegisterCompleteObjectUndo(objects, undoString);
        }
    }
}
