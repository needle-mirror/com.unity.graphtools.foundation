using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public delegate void CommandHandler<TState, in TCommand>(TState graphToolState, TCommand command)
        where TState : GraphToolState
        where TCommand : Command;

    [Obsolete("2021-01-05 Store was renamed to CommandDispatcher (UnityUpgradable) -> CommandDispatcher", true)]
    public sealed class Store
    {
        public Store(State state)
        {
        }
    }

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
                if (graphToolState.Preferences?.GetBool(BoolPref.LogAllDispatchedActions) ?? false)
                {
                    Debug.Log(command.GetType().FullName + " => " + m_Callback.Method.DeclaringType + "." + m_Callback.Method.Name);
                }

                m_Callback(graphToolState as TState, command as TCommand);
            }
        }

        readonly object m_SyncRoot = new object();

        readonly Dictionary<Type, CommandHandlerFunctorBase> m_CommandHandlers = new Dictionary<Type, CommandHandlerFunctorBase>();

        readonly List<Action<Command>> m_PreObservers = new List<Action<Command>>();
        readonly List<Action<Command>> m_PostObservers = new List<Action<Command>>();

        UndoRedoTraversal m_UndoRedoTraversal;

        bool m_ViewIsUpdating;
        CommandDispatchCheck m_CommandDispatchCheck;

        bool m_Disposed;

        public GraphToolState GraphToolState { get; }

        public CommandDispatcher(GraphToolState initialGraphToolState)
        {
            GraphToolState = initialGraphToolState;
            m_CommandDispatchCheck = new CommandDispatchCheck();

            Undo.undoRedoPerformed += UndoRedoPerformed;
        }

        ~CommandDispatcher()
        {
            Dispose(false);
        }

        public void RegisterCommandHandler<TCommand>(CommandHandler<GraphToolState, TCommand> commandHandler)
            where TCommand : Command
        {
            RegisterCommandHandler<GraphToolState, TCommand>(commandHandler);
        }

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

        public void UnregisterCommandHandler<TCommand>() where TCommand : Command
        {
            lock (m_SyncRoot)
            {
                m_CommandHandlers.Remove(typeof(TCommand));
            }
        }

        public void RegisterObserver(Action<Command> observer, bool asPostCommandObserver = false)
        {
            lock (m_SyncRoot)
            {
                if (asPostCommandObserver)
                {
                    if (m_PostObservers.Contains(observer))
                        throw new InvalidOperationException("Cannot register the same observer twice.");
                    m_PostObservers.Add(observer);
                }
                else
                {
                    if (m_PreObservers.Contains(observer))
                        throw new InvalidOperationException("Cannot register the same observer twice.");
                    m_PreObservers.Add(observer);
                }
            }
        }

        public void UnregisterObserver(Action<Command> observer, bool asPostCommandObserver = false)
        {
            lock (m_SyncRoot)
            {
                if (asPostCommandObserver)
                {
                    if (m_PostObservers.Contains(observer))
                    {
                        m_PostObservers.Remove(observer);
                    }
                }
                else
                {
                    if (m_PreObservers.Contains(observer))
                    {
                        m_PreObservers.Remove(observer);
                    }
                }
            }
        }

        public void Dispatch<TCommand>(TCommand command) where TCommand : Command
        {
            BeginStateChange();

            try
            {
                lock (m_SyncRoot)
                {
                    m_CommandDispatchCheck.BeginDispatch(command, GraphToolState.Preferences);

                    foreach (var observer in m_PreObservers)
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

                    foreach (var observer in m_PostObservers)
                    {
                        observer(command);
                    }
                }
            }
            finally
            {
                m_CommandDispatchCheck.EndDispatch();
                EndStateChange();
            }
        }

        public void MarkStateDirty()
        {
            BeginStateChange();
            GraphToolState.RequestUIRebuild();
            EndStateChange();
        }

        public void BeginStateChange()
        {
            Debug.Assert(!m_ViewIsUpdating);
        }

        public void EndStateChange()
        {
            GraphToolState.IncrementVersion();
        }

        public void BeginViewUpdate()
        {
            Debug.Assert(!m_ViewIsUpdating);

            m_ViewIsUpdating = true;
            m_CommandDispatchCheck.UpdateCounter++;
        }

        public uint EndViewUpdate()
        {
            Debug.Assert(m_ViewIsUpdating);

            m_ViewIsUpdating = false;
            GraphToolState.ResetChangeList();
            return GraphToolState.Version;
        }

        void UndoRedoPerformed()
        {
            var graphModel = GraphToolState.GraphModel;
            if (graphModel != null)
            {
                graphModel.UndoRedoPerformed();
                if (m_UndoRedoTraversal == null)
                    m_UndoRedoTraversal = new UndoRedoTraversal();
                m_UndoRedoTraversal.VisitGraph(graphModel);
            }
        }

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
