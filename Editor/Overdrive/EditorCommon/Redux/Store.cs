using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public delegate void Reducer<TState, in TAction>(TState previousState, TAction action)
        where TState : State
        where TAction : BaseAction;

    public sealed class Store : IDisposable
    {
        abstract class ReducerFunctorBase
        {
            public abstract void Invoke(State state, BaseAction action);
        }

        class ReducerFunctor<TState, TAction> : ReducerFunctorBase where TState : State where TAction : BaseAction
        {
            Reducer<TState, TAction> m_Callback;

            public ReducerFunctor(Reducer<TState, TAction> callback)
            {
                m_Callback = callback;
            }

            public override void Invoke(State state, BaseAction action)
            {
                if (state.Preferences?.GetBool(BoolPref.LogAllDispatchedActions) ?? false)
                {
                    Debug.Log(action.GetType().FullName + " => " + m_Callback.Method.DeclaringType + "." + m_Callback.Method.Name);
                }

                m_Callback(state as TState, action as TAction);
            }
        }

        readonly object m_SyncRoot = new object();

        readonly Dictionary<Type, ReducerFunctorBase> m_Reducers = new Dictionary<Type, ReducerFunctorBase>();

        readonly List<Action<BaseAction>> m_PreObservers = new List<Action<BaseAction>>();
        readonly List<Action<BaseAction>> m_PostObservers = new List<Action<BaseAction>>();

        UndoRedoTraversal m_UndoRedoTraversal;

        bool m_ViewIsUpdating;
        StoreDispatchCheck m_StoreDispatchCheck;

        bool m_Disposed;

        public State State { get; }

        public Store(State initialState)
        {
            State = initialState;
            m_StoreDispatchCheck = new StoreDispatchCheck();

            Undo.undoRedoPerformed += UndoRedoPerformed;
        }

        ~Store()
        {
            Dispose(false);
        }

        public void RegisterReducer<TAction>(Reducer<State, TAction> reducer)
            where TAction : BaseAction
        {
            RegisterReducer<State, TAction>(reducer);
        }

        public void RegisterReducer<TState, TAction>(Reducer<TState, TAction> reducer)
            where TState : State
            where TAction : BaseAction
        {
            lock (m_SyncRoot)
            {
                Type actionType = typeof(TAction);
                m_Reducers[actionType] = new ReducerFunctor<TState, TAction>(reducer);
            }
        }

        public void UnregisterReducer<TAction>() where TAction : BaseAction
        {
            lock (m_SyncRoot)
            {
                m_Reducers.Remove(typeof(TAction));
            }
        }

        public void RegisterObserver(Action<BaseAction> observer, bool asPostActionObserver = false)
        {
            lock (m_SyncRoot)
            {
                if (asPostActionObserver)
                {
                    if (m_PostObservers.Contains(observer))
                        throw new InvalidOperationException("Redux: Cannot register the same observer twice.");
                    m_PostObservers.Add(observer);
                }
                else
                {
                    if (m_PreObservers.Contains(observer))
                        throw new InvalidOperationException("Redux: Cannot register the same observer twice.");
                    m_PreObservers.Add(observer);
                }
            }
        }

        public void UnregisterObserver(Action<BaseAction> observer, bool asPostActionObserver = false)
        {
            lock (m_SyncRoot)
            {
                if (asPostActionObserver)
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

        public void Dispatch<TAction>(TAction action) where TAction : BaseAction
        {
            BeginStateChange();

            try
            {
                lock (m_SyncRoot)
                {
                    m_StoreDispatchCheck.BeginDispatch(action, State.Preferences);

                    foreach (Action<BaseAction> observer in m_PreObservers)
                    {
                        observer(action);
                    }

                    State.PreDispatchAction(action);

                    if (!m_Reducers.TryGetValue(action.GetType(), out var o))
                    {
                        Debug.LogError($"No reducer for action type {action.GetType()}");
                        return;
                    }

                    o.Invoke(State, action);

                    State.PostDispatchAction(action);

                    foreach (Action<BaseAction> observer in m_PostObservers)
                    {
                        observer(action);
                    }
                }
            }
            finally
            {
                m_StoreDispatchCheck.EndDispatch();
                EndStateChange();
            }
        }

        public void MarkStateDirty()
        {
            BeginStateChange();
            State.RequestUIRebuild();
            EndStateChange();
        }

        public void BeginStateChange()
        {
            Debug.Assert(!m_ViewIsUpdating);
        }

        public void EndStateChange()
        {
            State.IncrementVersion();
        }

        public void BeginViewUpdate()
        {
            Debug.Assert(!m_ViewIsUpdating);

            m_ViewIsUpdating = true;
            m_StoreDispatchCheck.UpdateCounter++;
        }

        public uint EndViewUpdate()
        {
            Debug.Assert(m_ViewIsUpdating);

            m_ViewIsUpdating = false;
            State.ResetChangeList();
            return State.Version;
        }

        void UndoRedoPerformed()
        {
            var graphModel = State.GraphModel;
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

                State?.Dispose();
            }

            m_Disposed = true;
        }
    }
}
