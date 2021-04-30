using System;

namespace UnityEngine.GraphToolsFoundation.CommandStateObserver
{
    /// <summary>
    /// Base class for undoable commands.
    /// </summary>
    public abstract class UndoableCommand : ICommand
    {
        /// <summary>
        /// The string that should appear in the Edit/Undo menu after this command is executed.
        /// </summary>
        public string UndoString { get; set; }
    }

    [Obsolete("Use UndoableCommand. Added in 0.9+. (UnityUpgradable) -> UndoableCommand")]
    public abstract class Command : UndoableCommand { }
}
