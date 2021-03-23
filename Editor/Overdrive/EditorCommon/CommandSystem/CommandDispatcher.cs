using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A function to handle a command.
    /// </summary>
    /// <param name="graphToolState">The state of the graph tool.</param>
    /// <param name="command">The command that needs to be handled.</param>
    /// <typeparam name="TState">The state type.</typeparam>
    /// <typeparam name="TCommand">The command type.</typeparam>
    public delegate void CommandHandler<TState, in TCommand>(TState graphToolState, TCommand command)
        where TState : GraphToolState
        where TCommand : Command;

    class CommandDispatcherFileManipulationProcessor : AssetModificationProcessor
    {
        const string k_AssetExtension = ".asset";

        static string[] OnWillSaveAssets(string[] paths)
        {
            foreach (var path in paths)
            {
                if (path.EndsWith(k_AssetExtension))
                {
                    var graphModel = AssetDatabase.LoadAssetAtPath<GraphAssetModel>(path);

                    if (graphModel != null)
                        graphModel.Dirty = false;
                }
            }
            return paths;
        }
    }

    /// <summary>
    /// The command dispatcher.
    /// </summary>
    public sealed class CommandDispatcher : IDisposable
    {
        abstract class CommandHandlerFunctorBase
        {
            public abstract void Invoke(GraphToolState graphToolState, Command command);
        }

        class CommandHandlerFunctor<TState, TCommand> : CommandHandlerFunctorBase where TState : GraphToolState where TCommand : Command
        {
            CommandHandler<TState, TCommand> m_Callback;

            public CommandHandlerFunctor(CommandHandler<TState, TCommand> callback)
            {
                m_Callback = callback;
            }

            public override void Invoke(GraphToolState graphToolState, Command command)
            {
                if (graphToolState.Preferences?.GetBool(BoolPref.LogAllDispatchedCommands) ?? false)
                {
                    Debug.Log(command.GetType().FullName + " => " + m_Callback.Method.DeclaringType + "." + m_Callback.Method.Name);
                }

                m_Callback(graphToolState as TState, command as TCommand);
            }
        }

        readonly object m_SyncRoot = new object();

        readonly Dictionary<Type, CommandHandlerFunctorBase> m_CommandHandlers = new Dictionary<Type, CommandHandlerFunctorBase>();

        readonly List<Action<Command>> m_CommandObservers = new List<Action<Command>>();

        readonly Dictionary<string, List<IStateObserver>> m_StateObservers = new Dictionary<string, List<IStateObserver>>();
        readonly HashSet<IStateObserver> m_ObserverCallSet = new HashSet<IStateObserver>();

        CommandDispatchCheck m_CommandDispatchCheck;

        bool m_Disposed;

        /// <summary>
        /// The tool state.
        /// </summary>
        public GraphToolState GraphToolState { get; }

        /// <summary>
        /// Initializes a new instance of the CommandDispatcher class.
        /// </summary>
        /// <param name="initialGraphToolState">The state.</param>
        public CommandDispatcher(GraphToolState initialGraphToolState)
        {
            GraphToolState = initialGraphToolState;
            m_CommandDispatchCheck = new CommandDispatchCheck();

            Undo.undoRedoPerformed += UndoRedoPerformed;

            RegisterCommandHandler<UndoRedoCommand>(UndoRedoCommand.DefaultCommandHandler);
        }

        ~CommandDispatcher()
        {
            Dispose(false);
        }

        /// <summary>
        /// Registers a handler for a command type.
        /// </summary>
        /// <param name="commandHandler">The command handler.</param>
        /// <typeparam name="TCommand">The command type.</typeparam>
        public void RegisterCommandHandler<TCommand>(CommandHandler<GraphToolState, TCommand> commandHandler)
            where TCommand : Command
        {
            RegisterCommandHandler<GraphToolState, TCommand>(commandHandler);
        }

        /// <summary>
        /// Registers a handler for a command type.
        /// </summary>
        /// <param name="commandHandler">The command handler.</param>
        /// <typeparam name="TCommand">The command type.</typeparam>
        /// <typeparam name="TState">The state type.</typeparam>
        public void RegisterCommandHandler<TState, TCommand>(CommandHandler<TState, TCommand> commandHandler)
            where TState : GraphToolState
            where TCommand : Command
        {
            lock (m_SyncRoot)
            {
                var commandType = typeof(TCommand);
                m_CommandHandlers[commandType] = new CommandHandlerFunctor<TState, TCommand>(commandHandler);
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
        public void UnregisterCommandHandler<TCommand>() where TCommand : Command
        {
            lock (m_SyncRoot)
            {
                m_CommandHandlers.Remove(typeof(TCommand));
            }
        }

        /// <summary>
        /// Registers a command observer.
        /// </summary>
        /// <remarks>
        /// The command observer will be called whenever a command is dispatched.
        /// </remarks>
        /// <param name="observer">The observer.</param>
        /// <exception cref="InvalidOperationException">Thrown when the observer is already registered.</exception>
        public void RegisterCommandObserver(Action<Command> observer)
        {
            lock (m_SyncRoot)
            {
                if (m_CommandObservers.Contains(observer))
                    throw new InvalidOperationException("Cannot register the same observer twice.");
                m_CommandObservers.Add(observer);
            }
        }

        /// <summary>
        /// Unregisters a command observer.
        /// </summary>
        /// <param name="observer">The observer.</param>
        /// <exception cref="InvalidOperationException"></exception>
        public void UnregisterCommandObserver(Action<Command> observer)
        {
            lock (m_SyncRoot)
            {
                if (m_CommandObservers.Contains(observer))
                {
                    m_CommandObservers.Remove(observer);
                }
            }
        }

        /// <summary>
        /// Registers a state observer.
        /// </summary>
        /// <param name="observer">The observer.</param>
        /// <exception cref="InvalidOperationException">Thrown when the observer is already registered.</exception>
        public void RegisterObserver(IStateObserver observer)
        {
            if (observer == null)
                return;

            lock (m_SyncRoot)
            {
                foreach (var component in observer.ObservedStateComponents)
                {
                    if (!m_StateObservers.TryGetValue(component, out var observerForComponent))
                    {
                        observerForComponent = new List<IStateObserver>();
                        m_StateObservers[component] = observerForComponent;
                    }

                    if (observerForComponent.Contains(observer))
                        throw new InvalidOperationException("Cannot register the same observer twice.");
                    observerForComponent.Add(observer);
                }
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

            lock (m_SyncRoot)
            {
                // We do it this way in case observer.ObservedStateComponents changed since RegisterObserver() was called.
                foreach (var observersByComponent in m_StateObservers)
                {
                    observersByComponent.Value.Remove(observer);
                }
            }
        }

        /// <summary>
        /// Dispatches a command. This will call all command observers; then the command handler
        /// registered for this command will be executed.
        /// </summary>
        /// <param name="command">The command to dispatch.</param>
        public void Dispatch(Command command)
        {
            try
            {
                lock (m_SyncRoot)
                {
                    m_CommandDispatchCheck.BeginDispatch(command, GraphToolState.Preferences);

                    foreach (var observer in m_CommandObservers)
                    {
                        observer(command);
                    }

                    GraphToolState.PreDispatchCommand(command);

                    if (!m_CommandHandlers.TryGetValue(command.GetType(), out var o))
                    {
                        Debug.LogError($"No handler for command type {command.GetType()}");
                        return;
                    }

                    o.Invoke(GraphToolState, command);

                    GraphToolState.PostDispatchCommand(command);
                }
            }
            finally
            {
                m_CommandDispatchCheck.EndDispatch();
            }
        }

        internal IEnumerable<IStateObserver> SortObservers(IReadOnlyCollection<IStateObserver> observers)
        {
            var remainingObservers = observers.ToList();
            var modifiedStates = observers.SelectMany(observer => observer.ModifiedStateComponents).ToList();

            var cycleDetected = false;
            while (remainingObservers.Count > 0 && !cycleDetected)
            {
                var eligibleObservers = remainingObservers.Count;
                for (var index = remainingObservers.Count - 1; index >= 0; index--)
                {
                    var observer = remainingObservers[index];

                    if (observer.ObservedStateComponents.Any(observedStateComponent => modifiedStates.Contains(observedStateComponent)))
                    {
                        eligibleObservers--;
                    }
                    else
                    {
                        foreach (var modifiedStateComponent in observer.ModifiedStateComponents)
                        {
                            modifiedStates.Remove(modifiedStateComponent);
                        }

                        remainingObservers.RemoveAt(index);
                        yield return observer;
                    }
                }

                cycleDetected = eligibleObservers == 0;
            }

            if (remainingObservers.Count > 0)
            {
                Debug.LogWarning("Dependency cycle detected in observers.");
                foreach (var observer in remainingObservers)
                {
                    yield return observer;
                }
            }
        }

        /// <summary>
        /// Notifies state observers that the state has changed.
        /// </summary>
        /// <remarks>
        /// State observers will only be notified if the state components they are observing have changed.
        /// </remarks>
        public void NotifyObservers()
        {
            lock (m_SyncRoot)
            {
                m_ObserverCallSet.Clear();
                foreach (var editorStateComponent in GraphToolState.AllStateComponents)
                {
                    if (editorStateComponent.HasChanges() &&
                        m_StateObservers.TryGetValue(editorStateComponent.StateSlotName, out var observersForComponent))
                    {
                        m_ObserverCallSet.AddRange(observersForComponent);
                    }
                }

                var sortedObservers = SortObservers(m_ObserverCallSet);
                foreach (var observer in sortedObservers)
                {
                    StateObserverHelper.CurrentObserver = observer;
                    observer.Observe(GraphToolState);
                }

                StateObserverHelper.CurrentObserver = null;

                // For each state component, find the earliest observed version in all observers and purge the
                // changesets that are earlier than this earliest version.
                foreach (var editorStateComponent in GraphToolState.AllStateComponents)
                {
                    var stateComponentName = editorStateComponent.StateSlotName;
                    var earliestObserverVersion = uint.MaxValue;

                    if (m_StateObservers.TryGetValue(stateComponentName, out var observersForComponent))
                    {
                        if (observersForComponent.Any())
                        {
                            earliestObserverVersion = observersForComponent.Min(
                                o =>
                                {
                                    var v = o.GetLastObservedComponentVersion(stateComponentName);
                                    return v.HashCode == editorStateComponent.GetHashCode() ? v.Version : uint.MinValue;
                                });
                        }
                    }

                    editorStateComponent.PurgeOldChangesets(earliestObserverVersion);
                }
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
                    stateObserver.UpdateObservedVersion(observerEntry.Key, default);
                }
            }
        }

        void UndoRedoPerformed()
        {
            Dispatch(new UndoRedoCommand());
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (m_Disposed)
                return;

            // Dispose of unmanaged resources here

            if (disposing)
            {
                // Dispose of managed resources here.
                // Call members' Dispose()

                // ReSharper disable once DelegateSubtraction
                Undo.undoRedoPerformed -= UndoRedoPerformed;

                GraphToolState?.Dispose();
            }

            m_Disposed = true;
        }
    }
}
