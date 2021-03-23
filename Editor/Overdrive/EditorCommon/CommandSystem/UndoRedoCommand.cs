using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Command sent on undo/redo.
    /// </summary>
    public class UndoRedoCommand : Command
    {
        /// <summary>
        /// Initializes a new instance of the UndoRedoCommand class.
        /// </summary>
        public UndoRedoCommand()
        {
            UndoString = "Undo";
        }

        /// <summary>
        /// Default command handler for undo/redo.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(GraphToolState state, UndoRedoCommand command)
        {
            var graphModel = state.WindowState.GraphModel;
            if (graphModel != null)
            {
                graphModel.UndoRedoPerformed();
            }
        }
    }
}
