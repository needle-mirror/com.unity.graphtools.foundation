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
                m_Callback(state as TState, action as TAction);
            }
        }

        readonly object m_SyncRoot = new object();

        readonly Dictionary<Type, ReducerFunctorBase> m_Reducers = new Dictionary<Type, ReducerFunctorBase>();

        readonly List<Action<BaseAction>> m_Observers = new List<Action<BaseAction>>();

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

        public Store(State initialState)
        {
            m_LastState = initialState;
            m_StoreDispatchCheck = new StoreDispatchCheck();

            Undo.undoRedoPerformed += UndoRedoPerformed;
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

        public void RegisterObserver(Action<BaseAction> observer)
        {
            lock (m_SyncRoot)
            {
                if (m_Observers.Contains(observer))
                    throw new InvalidOperationException("Redux: Cannot register the same observer twice.");
                m_Observers.Add(observer);
            }
        }

        public void UnregisterObserver(Action<BaseAction> observer)
        {
            lock (m_SyncRoot)
            {
                if (m_Observers.Contains(observer))
                {
                    m_Observers.Remove(observer);
                }
            }
        }

        public void Dispatch<TAction>(TAction action) where TAction : BaseAction
        {
            Preferences preferences = GetState().Preferences;

            if (preferences != null && preferences.GetBool(BoolPref.LogAllDispatchedActions))
                Debug.Log(action);

            try
            {
                lock (m_SyncRoot)
                {
                    m_StoreDispatchCheck.BeginDispatch(action, preferences);

                    foreach (Action<BaseAction> observer in m_Observers)
                    {
                        observer(action);
                    }

                    m_LastState.PreDispatchAction(action);

                    if (!m_Reducers.TryGetValue(action.GetType(), out var o))
                    {
                        Debug.LogError($"No reducer for action type {action.GetType()}");
                        return;
                    }

                    o.Invoke(m_LastState, action);
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
