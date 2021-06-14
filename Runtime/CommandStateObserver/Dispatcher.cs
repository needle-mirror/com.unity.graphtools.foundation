using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.GraphToolsFoundation.CommandStateObserver
{
    /// <summary>
    /// Diagnostic flags for command dispatch.
    /// </summary>
    [Flags]
    public enum Diagnostics
    {
        /// <summary>
        /// No diagnostic done when dispatching.
        /// </summary>
        None = 0,

        /// <summary>
        /// Log all dispatched commands and their handler.
        /// </summary>
        LogAllCommands = 1 << 0,

        /// <summary>
        /// Log an error when a command is dispatched while dispatching another command.
        /// </summary>
        CheckRecursiveDispatch = 1 << 1,
    }

    /// <summary>
    /// A function to handle a command.
    /// </summary>
    /// <param name="state">The state.</param>
    /// <param name="command">The command that needs to be handled.</param>
    /// <typeparam name="TState">The state type.</typeparam>
    /// <typeparam name="TCommand">The command type.</typeparam>
    public delegate void CommandHandler<in TState, in TCommand>(TState state, TCommand command)
        where TState : IState
        where TCommand : ICommand;

    /// <summary>
    /// The command dispatcher.
    /// </summary>
    public class Dispatcher : IDisposable
    {
        /// <summary>
        /// Class to wrap a command handler and invoke it.
        /// </summary>
        protected abstract class CommandHandlerFunctorBase
        {
            public abstract void Invoke(IState state, ICommand command, bool logHandler);
        }

        class CommandHandlerFunctor<TState, TCommand> : CommandHandlerFunctorBase
            where TState : IState
            where TCommand : ICommand
        {
            CommandHandler<TState, TCommand> m_Callback;

            public CommandHandlerFunctor(CommandHandler<TState, TCommand> callback)
            {
                m_Callback = callback;
            }

            public override void Invoke(IState state, ICommand command, bool logHandler)
            {
                if (logHandler)
                {
                    Debug.Log($"{command.GetType().FullName} => {m_Callback.Method.DeclaringType}.{m_Callback.Method.Name}");
                }

                if (state is TState tState && command is TCommand tCommand)
                {
                    m_Callback(tState, tCommand);
                }
                else
                {
                    if (!(state is TState))
                        Debug.Log($"Type mismatch: {state.GetType()} is not a {typeof(TState)}.");
                    if (!(command is TCommand))
                        Debug.Log($"Type mismatch: {command.GetType()} is not a {typeof(TCommand)}.");
                }
            }
        }

        /// <summary>
        /// The mapping of command types to command handlers.
        /// </summary>
        protected readonly Dictionary<Type, CommandHandlerFunctorBase> m_CommandHandlers = new Dictionary<Type, CommandHandlerFunctorBase>();
        /// <summary>
        /// The list of actions that needs to be invoked before executing a command.
        /// </summary>
        protected readonly List<Action<ICommand>> m_CommandPreDispatchCallbacks = new List<Action<ICommand>>();
        /// <summary>
        /// A mapping of state component names to observers observing those components.
        /// </summary>
        protected readonly Dictionary<string, List<IStateObserver>> m_StateObservers = new Dictionary<string, List<IStateObserver>>();

        List<IStateObserver> m_SortedObservers = new List<IStateObserver>();
        readonly HashSet<IStateObserver> m_ObserverCallSet = new HashSet<IStateObserver>();
        readonly HashSet<string> m_DirtyComponentSet = new HashSet<string>();

        /// <summary>
        /// The command being executed.
        /// </summary>
        protected ICommand m_CurrentCommand;
        bool m_Disposed;

        /// <summary>
        /// The state.
        /// </summary>
        public IState State { get; }

        /// <summary>
        /// Returns true is a command is being dispatched.
        /// </summary>
        bool IsDispatching => m_CurrentCommand != null;
        /// <summary>
        /// Returns true if the observers are being notified.
        /// </summary>
        bool IsObserving { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Dispatcher" /> class.
        /// </summary>
        /// <param name="state">The state.</param>
        public Dispatcher(IState state)
        {
            Undo.undoRedoPerformed += UndoRedoPerformed;
            State = state;
            state.RegisterCommandHandlers(this);
        }

        ~Dispatcher()
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
        /// Dispose implementation.
        /// </summary>
        /// <param name="disposing">When true, this method is called from IDisposable.Dispose.
        /// Otherwise it is called from the finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (m_Disposed)
                return;

            // Dispose of unmanaged resources here

            if (disposing)
            {
                // Dispose of managed resources here.
                // Call members' Dispose()

                Undo.undoRedoPerformed -= UndoRedoPerformed;

                if (State is IDisposable disposableState)
                    disposableState.Dispose();
            }

            m_Disposed = true;
        }

        /// <summary>
        /// Registers a handler for a command type.
        /// </summary>
        /// <param name="commandHandler">The command handler.</param>
        /// <typeparam name="TCommand">The command type.</typeparam>
        public void RegisterCommandHandler<TCommand>(CommandHandler<IState, TCommand> commandHandler)
            where TCommand : ICommand
        {
            RegisterCommandHandler<IState, TCommand>(commandHandler);
        }

        /// <summary>
        /// Registers a handler for a command type.
        /// </summary>
        /// <param name="commandHandler">The command handler.</param>
        /// <typeparam name="TCommand">The command type.</typeparam>
        /// <typeparam name="TState">The state type.</typeparam>
        public void RegisterCommandHandler<TState, TCommand>(CommandHandler<TState, TCommand> commandHandler)
            where TState : IState
            where TCommand : ICommand
        {
            if (!IsDispatching)
            {
                var commandType = typeof(TCommand);
                m_CommandHandlers[commandType] = new CommandHandlerFunctor<TState, TCommand>(commandHandler);
            }
            else
            {
                Debug.LogError($"Cannot call {nameof(RegisterCommandHandler)} while dispatching a command.");
            }
        }

        /// <summary>
        /// Unregisters the command handler for a command type.
        /// </summary>
        /// <remarks>
        /// Since there is only one command handler registered for a command type, it is not necessary
        /// to specify the command handler to unregister.
        /// </remarks>
        /// <typeparam name="TCommand">The command type.</typeparam>
        public void UnregisterCommandHandler<TCommand>() where TCommand : ICommand
        {
            if (!IsDispatching)
            {
                m_CommandHandlers.Remove(typeof(TCommand));
            }
            else
            {
                Debug.LogError($"Cannot call {nameof(UnregisterCommandHandler)} while dispatching a command.");
            }
        }

        /// <summary>
        /// Registers a command observer.
        /// </summary>
        /// <remarks>
        /// The command observer will be called whenever a command is dispatched.
        /// </remarks>
        /// <param name="callback">The observer.</param>
        /// <exception cref="InvalidOperationException">Thrown when the observer is already registered.</exception>
        public void RegisterCommandPreDispatchCallback(Action<ICommand> callback)
        {
            if (!IsDispatching)
            {
                if (m_CommandPreDispatchCallbacks.Contains(callback))
                    throw new InvalidOperationException("Cannot register the same observer twice.");
                m_CommandPreDispatchCallbacks.Add(callback);
            }
            else
            {
                Debug.LogError($"Cannot call {nameof(RegisterCommandPreDispatchCallback)} while dispatching a command.");
            }
        }

        /// <summary>
        /// Unregisters a command observer.
        /// </summary>
        /// <param name="callback">The observer.</param>
        /// <exception cref="InvalidOperationException"></exception>
        public void UnregisterCommandPreDispatchCallback(Action<ICommand> callback)
        {
            if (!IsDispatching)
            {
                m_CommandPreDispatchCallbacks.Remove(callback);
            }
            else
            {
                Debug.LogError($"Cannot call {nameof(UnregisterCommandPreDispatchCallback)} while dispatching a command.");
            }
        }

        /// <summary>
        /// Registers a state observer.
        /// </summary>
        /// <remarks>
        /// The content of <see cref="StateObserver{TState}.ObservedStateComponents"/> and
        /// <see cref="StateObserver{TState}.ModifiedStateComponents"/> should not change once the
        /// observer is registered.
        /// </remarks>
        /// <param name="observer">The observer.</param>
        /// <param name="allowDuplicateType">Set to true to allow registering more than one observer of the same type. Default to false.</param>
        /// <exception cref="InvalidOperationException">Thrown when the observer is already registered.</exception>
        public void RegisterObserver(IStateObserver observer, bool allowDuplicateType = false)
        {
            if (observer == null)
                return;

            foreach (var component in observer.ObservedStateComponents)
            {
                if (!m_StateObservers.TryGetValue(component, out var observerForComponent))
                {
                    observerForComponent = new List<IStateObserver>();
                    m_StateObservers[component] = observerForComponent;
                }

                if (observerForComponent.Contains(observer))
                    throw new InvalidOperationException("Cannot register the same observer twice.");

                if (!allowDuplicateType && observerForComponent.Select(o => o.GetType()).Contains(observer.GetType()))
                    Debug.LogWarning($"There is already a registered observer of type {observer.GetType().FullName}. Make sure you are not unintentionally registering the same observer twice.");

                observerForComponent.Add(observer);
                m_SortedObservers = null;
            }
        }

        /// <summary>
        /// Unregisters a state observer.
        /// </summary>
        /// <param name="observer">The observer.</param>
        public void UnregisterObserver(IStateObserver observer)
        {
            if (observer == null)
                return;

            // We do it this way in case observer.ObservedStateComponents changed since RegisterObserver() was called.
            foreach (var observersByComponent in m_StateObservers)
            {
                observersByComponent.Value.Remove(observer);
                m_SortedObservers = null;
            }
        }

        /// <summary>
        /// Resets the observed version of all observers to the default value, making them out of date for
        /// all their observed state components.
        /// </summary>
        public void InvalidateAllObservers()
        {
            foreach (var observerEntry in m_StateObservers)
            {
                foreach (var stateObserver in observerEntry.Value)
                {
                    (stateObserver as IInternalStateObserver)?.UpdateObservedVersion(observerEntry.Key, default);
                }
            }
        }

        /// <summary>
        /// Dispatches a command. This will call all command observers; then the command handler
        /// registered for this command will be executed.
        /// </summary>
        /// <param name="command">The command to dispatch.</param>
        public virtual void Dispatch(ICommand command)
        {
            if (IsDispatching && IsDiagnosticFlagSet(Diagnostics.CheckRecursiveDispatch))
            {
                Debug.LogError($"Recursive dispatch detected: command {command.GetType().Name} dispatched during {m_CurrentCommand.GetType().Name}'s dispatch");
            }

            if (!m_CommandHandlers.TryGetValue(command.GetType(), out var handler))
            {
                Debug.LogError($"No handler for command type {command.GetType()}");
                return;
            }

            try
            {
                m_CurrentCommand = command;

                foreach (var callback in m_CommandPreDispatchCallbacks)
                {
                    callback(command);
                }

                PreDispatchCommand(command);

                try
                {
                    var logHandler = IsDiagnosticFlagSet(Diagnostics.LogAllCommands);
                    handler.Invoke(State, command, logHandler);
                }
                finally
                {
                    PostDispatchCommand(command);
                }
            }
            finally
            {
                m_CurrentCommand = null;
            }
        }

        /// <summary>
        /// Called when a command is dispatched, before the command handler is executed.
        /// </summary>
        /// <param name="command">The command being dispatched.</param>
        protected virtual void PreDispatchCommand(ICommand command)
        {
        }

        /// <summary>
        /// Called when a command is dispatched, after the command handler has been executed.
        /// </summary>
        /// <param name="command">The command being dispatched.</param>
        protected virtual void PostDispatchCommand(ICommand command)
        {
        }

        void SortObservers()
        {
            var observers = m_StateObservers.Values.SelectMany(x => x)
                .Distinct()
                .ToList();

            SortObservers(observers, out m_SortedObservers);
        }

        // Will modify observersToSort.
        internal static void SortObservers(List<IStateObserver> observersToSort, out List<IStateObserver> sortedObservers)
        {
            sortedObservers = new List<IStateObserver>(observersToSort.Count);
            var modifiedStates = observersToSort.SelectMany(observer => observer.ModifiedStateComponents).ToList();

            var cycleDetected = false;
            while (observersToSort.Count > 0 && !cycleDetected)
            {
                var remainingObserverCount = observersToSort.Count;
                for (var index = observersToSort.Count - 1; index >= 0; index--)
                {
                    var observer = observersToSort[index];

                    if (observer.ObservedStateComponents.Any(observedStateComponent => modifiedStates.Contains(observedStateComponent)))
                    {
                        remainingObserverCount--;
                    }
                    else
                    {
                        foreach (var modifiedStateComponent in observer.ModifiedStateComponents)
                        {
                            modifiedStates.Remove(modifiedStateComponent);
                        }

                        observersToSort.RemoveAt(index);
                        sortedObservers.Add(observer);
                    }
                }

                cycleDetected = remainingObserverCount == 0;
            }

            if (observersToSort.Count > 0)
            {
                Debug.LogWarning("Dependency cycle detected in observers.");
                sortedObservers.AddRange(observersToSort);
            }
        }

        /// <summary>
        /// Notifies state observers that the state has changed.
        /// </summary>
        /// <remarks>
        /// State observers will only be notified if the state components they are observing have changed.
        /// </remarks>
        public virtual void NotifyObservers()
        {
            if (!IsObserving)
            {
                try
                {
                    IsObserving = true;

                    if (m_SortedObservers == null)
                        SortObservers();

                    m_ObserverCallSet.Clear();
                    if (m_SortedObservers.Count > 0)
                    {
                        m_DirtyComponentSet.Clear();

                        // Using for loop to avoid LINQ allocations.
                        foreach (var component in State.AllStateComponents)
                        {
                            if (component.HasChanges())
                            {
                                m_DirtyComponentSet.Add(component.StateSlotName);
                            }
                        }

                        if (m_DirtyComponentSet.Count > 0)
                        {
                            foreach (var observer in m_SortedObservers)
                            {
                                if (m_DirtyComponentSet.Overlaps(observer.ObservedStateComponents))
                                {
                                    m_ObserverCallSet.Add(observer);
                                    m_DirtyComponentSet.UnionWith(observer.ModifiedStateComponents);
                                }
                            }
                        }
                    }

                    if (m_ObserverCallSet.Any())
                    {
                        try
                        {
                            foreach (var observer in m_ObserverCallSet)
                            {
                                StateObserverHelper.CurrentObserver = observer;
                                observer.Observe(State);
                            }
                        }
                        finally
                        {
                            StateObserverHelper.CurrentObserver = null;
                        }

                        // If m_ObserverCallSet is empty, observed versions did not change, so changesets do not need to be purged.

                        // For each state component, find the earliest observed version in all observers and purge the
                        // changesets that are earlier than this earliest version.
                        foreach (var editorStateComponent in State.AllStateComponents)
                        {
                            var stateComponentName = editorStateComponent.StateSlotName;
                            var stateComponentHashCode = editorStateComponent.GetHashCode();

                            var earliestObservedVersion = uint.MaxValue;

                            if (m_StateObservers.TryGetValue(stateComponentName, out var observersForComponent))
                            {
                                // Not using List.Min to avoid closure allocation.
                                foreach (var observer in observersForComponent)
                                {
                                    var v = (observer as IInternalStateObserver)?.GetLastObservedComponentVersion(stateComponentName) ?? default;
                                    var versionNumber = v.HashCode == stateComponentHashCode ? v.Version : uint.MinValue;
                                    earliestObservedVersion = Math.Min(earliestObservedVersion, versionNumber);
                                }
                            }

                            editorStateComponent.PurgeOldChangesets(earliestObservedVersion);
                        }
                    }
                }
                finally
                {
                    m_ObserverCallSet.Clear();
                    m_DirtyComponentSet.Clear();
                    IsObserving = false;
                }
            }
        }

        void UndoRedoPerformed()
        {
            Dispatch(new UndoRedoCommand());
        }

        /// <summary>
        /// Checks whether a diagnostic flag is set.
        /// </summary>
        /// <param name="flag">The flag to check.</param>
        /// <returns>True if the flag is set, false otherwise.</returns>
        protected virtual bool IsDiagnosticFlagSet(Diagnostics flag)
        {
            return false;
        }
    }
}
