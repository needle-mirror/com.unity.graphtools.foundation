using System;

namespace UnityEngine.GraphToolsFoundation.CommandStateObserver
{
    /// <summary>
    /// Command sent on undo/redo.
    /// </summary>
    public class UndoRedoCommand : ICommand
    {
        /// <summary>
        /// Default command handler for undo/redo.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(IState state, UndoRedoCommand command)
        {
        }
    }
}
