using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.GraphToolsFoundation.CommandStateObserver
{
    /// <summary>
    /// The state holds all data that can be displayed in the UI and modified by the user.
    /// </summary>
    public abstract class State : IState, IDisposable
    {
        bool m_Disposed;

        UndoState m_UndoState;

        /// <summary>
        /// The persisted state.
        /// </summary>
        protected PersistedState PersistedState { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="State" /> class.
        /// </summary>
        protected State()
        {
            PersistedState = new PersistedState();

            m_UndoState = ScriptableObject.CreateInstance<UndoState>();
            m_UndoState.hideFlags = HideFlags.HideAndDontSave | HideFlags.HideInInspector;
            m_UndoState.State = this;
        }

        ~State()
        {
            Dispose(false);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of resources used by the state.
        /// </summary>
        /// <param name="disposing">When true, this method is called from IDisposable.Dispose.
        /// Otherwise it is called from the finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (m_Disposed)
                return;

            if (disposing)
            {
                foreach (var stateComponent in AllStateComponents)
                {
                    stateComponent.Dispose();
                }
            }

            m_Disposed = true;
        }

        /// <inheritdoc />
        public virtual void RegisterCommandHandlers(Dispatcher dispatcher)
        {
            dispatcher.RegisterCommandHandler<UndoRedoCommand>(UndoRedoCommand.DefaultCommandHandler);
        }

        /// <summary>
        /// Clears all caches in the state. Called whenever the underlying state data
        /// have been changed, for example after an undo.
        /// </summary>
        protected virtual void ResetStateCaches()
        {
            PersistedState.Flush();
        }

        /// <summary>
        /// Finishes the previous undo group, if there was one, and begin a new undo group.
        /// The name of the group is taken from the command argument.
        /// </summary>
        /// <remarks>
        /// The undo group is automatically incremented based on events
        /// eg. mouse down events, executing a menu item increments the current
        /// group. But sometimes it is necessary to manually start a new group of undo operations.
        /// </remarks>
        /// <param name="command">The command that causes the new undo group to be started.
        /// Pass null if the undo group should not be named.</param>
        internal void PushUndoGroup(UndoableCommand command = null)
        {
            Undo.IncrementCurrentGroup();
            if (command != null && !string.IsNullOrEmpty(command.UndoString))
                Undo.SetCurrentGroupName(command.UndoString);
        }

        /// <summary>
        /// Pushes the current state on the undo stack.
        /// </summary>
        /// <param name="command">Use command.UndoString as the name of the undo item.</param>
        public virtual void PushUndo(UndoableCommand command)
        {
            Undo.RegisterCompleteObjectUndo(new Object[] { m_UndoState }, command?.UndoString ?? "");
        }

        /// <summary>
        /// Serializes the state to push on the undo stack.
        /// </summary>
        /// <param name="stateComponentUndoData">A list to hold the serialized data.</param>
        protected internal virtual void SerializeForUndo(SerializedReferenceDictionary<string, string> stateComponentUndoData)
        {
        }

        /// <summary>
        /// Restores the state from its serialized data.
        /// </summary>
        /// <param name="stateComponentUndoData">The serialized state data.</param>
        protected internal virtual void DeserializeFromUndo(SerializedReferenceDictionary<string, string> stateComponentUndoData)
        {
            ResetStateCaches();
        }

        /// <summary>
        /// Performs state validation after deserialization.
        /// </summary>
        protected internal virtual void ValidateAfterDeserialize()
        {
        }

        /// <inheritdoc />
        public virtual IEnumerable<IStateComponent> AllStateComponents => Enumerable.Empty<IStateComponent>();
    }
}
