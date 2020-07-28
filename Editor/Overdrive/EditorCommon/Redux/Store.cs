using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public delegate TState Reducer<TState, in TAction>(TState previousState, TAction action)
        where TState : State
        where TAction : class, IAction;

    public sealed class Store : IDisposable
    {
        abstract class ReducerFunctorBase
        {
            public abstract State Invoke(State state, IAction action);
        }

        class ReducerFunctor<TState, TAction> : ReducerFunctorBase where TState : State where TAction : class, IAction
        {
            Reducer<TState, TAction> m_Callback;

            public ReducerFunctor(Reducer<TState, TAction> callback)
            {
                m_Callback = callback;
            }

            public override State Invoke(State state, IAction action)
            {
                return m_Callback(state as TState, action as TAction);
            }
        }

        readonly object m_SyncRoot = new object();

        readonly Dictionary<Type, ReducerFunctorBase> m_Reducers = new Dictionary<Type, ReducerFunctorBase>();

        readonly List<Action<IAction>> m_Observers = new List<Action<IAction>>();

        UndoRedoTraversal m_UndoRedoTraversal;

        State m_LastState;

        Action m_StateChanged;

        bool m_StateDirty;

        StoreDispatchCheck m_StoreDispatchCheck;

        public event Action StateChanged
        {
            add => m_StateChanged += value;
            // ReSharper disable once DelegateSubtraction
            remove => m_StateChanged -= value;
        }

        public Store(State initialState, Action<Store> registerActions)
        {
            m_LastState = initialState;
            m_StoreDispatchCheck = new StoreDispatchCheck();

            Undo.undoRedoPerformed += UndoRedoPerformed;

            registerActions?.Invoke(this);
        }

        public void RegisterReducer<TState, TAction>(Reducer<TState, TAction> reducer)
            where TState : State
            where TAction : class, IAction
        {
            lock (m_SyncRoot)
            {
                Type actionType = typeof(TAction);

                // PF: Accept reducer overrides for now. Need a better solution.
                //if (m_Reducers.ContainsKey(actionType))
                //    throw new InvalidOperationException("Redux: Cannot register two reducers for action " + actionType.Name);
                m_Reducers[actionType] = new ReducerFunctor<TState, TAction>(reducer);
            }
        }

        public void UnregisterReducer<TAction>() where TAction : IAction
        {
            lock (m_SyncRoot)
            {
                m_Reducers.Remove(typeof(TAction));
            }
        }

        public void RegisterObserver(Action<IAction> observer)
        {
            lock (m_SyncRoot)
            {
                if (m_Observers.Contains(observer))
                    throw new InvalidOperationException("Redux: Cannot register the same observer twice.");
                m_Observers.Add(observer);
            }
        }

        public void UnregisterObserver(Action<IAction> observer)
        {
            lock (m_SyncRoot)
            {
                if (m_Observers.Contains(observer))
                {
                    m_Observers.Remove(observer);
                }
            }
        }

        public void Dispatch<TAction>(TAction action) where TAction : IAction
        {
            Preferences preferences = GetState().Preferences;

            if (preferences != null && preferences.GetBool(BoolPref.LogAllDispatchedActions))
                Debug.Log(action);

            try
            {
                lock (m_SyncRoot)
                {
                    m_StoreDispatchCheck.BeginDispatch(action, preferences);

                    foreach (Action<IAction> observer in m_Observers)
                    {
                        observer(action);
                    }

                    m_LastState.PreDispatchAction(action);

                    if (!m_Reducers.TryGetValue(action.GetType(), out var o))
                    {
                        Debug.LogError($"No reducer for action type {action.GetType()}");
                        return;
                    }

                    m_LastState = o.Invoke(m_LastState, action);
                }
            }
            finally
            {
                m_StoreDispatchCheck.EndDispatch();
            }

            m_StateDirty = true;
        }

        public void ForceRefreshUI(UpdateFlags updateFlags)
        {
            // Resetting the change list is required to trigger a full UI rebuild, which is necessary to
            // update the position dependency manager.
            // PF: wat???
            m_LastState.CurrentGraphModel?.ResetChangeList();

            m_LastState.MarkForUpdate(updateFlags);
            m_StateDirty = true;
        }

        // Called once per frame
        public void Update()
        {
            m_StoreDispatchCheck.UpdateCounter++;

            if (m_StateDirty)
            {
                m_StateDirty = false;
                InvokeStateChanged();
            }
        }

        void UndoRedoPerformed()
        {
            var graphModel = GetState().CurrentGraphModel;
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
            // ReSharper disable once DelegateSubtraction
            Undo.undoRedoPerformed -= UndoRedoPerformed;

            m_LastState?.Dispose();
            m_StateChanged = null;
        }

        void InvokeStateChanged()
        {
            m_LastState.PreStateChanged();
            m_StateChanged?.Invoke();
            m_LastState.PostStateChanged();
        }

        public State GetState()
        {
            return m_LastState;
        }
    }
}
