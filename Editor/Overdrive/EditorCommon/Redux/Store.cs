using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public delegate TState Reducer<TState, in TAction>(TState previousState, TAction action)
        where TState : State
        where TAction : class, IAction;

    public class Store : IDisposable
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

        State m_LastState;
        Action m_StateChanged;
        bool m_StateDirty;

        protected Store(State initialState = default)
        {
            m_LastState = initialState;
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

        protected void ClearReducers()
        {
            m_Reducers.Clear();
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

        public event Action StateChanged
        {
            add => m_StateChanged += value;
            // ReSharper disable once DelegateSubtraction
            remove => m_StateChanged -= value;
        }

        public virtual void Dispatch<TAction>(TAction action) where TAction : IAction
        {
            lock (m_SyncRoot)
            {
                foreach (Action<IAction> observer in m_Observers)
                {
                    observer(action);
                }

                PreDispatchAction(action);


                if (!m_Reducers.TryGetValue(action.GetType(), out var o))
                {
                    Debug.LogError($"No reducer for action type {action.GetType()}");
                    return;
                }

                m_LastState = o.Invoke(m_LastState, action);
            }

            m_StateDirty = true;
        }

        // Called once per frame
        public void Update()
        {
            if (m_StateDirty)
            {
                m_StateDirty = false;
                InvokeStateChanged();
            }
        }

        public virtual void Dispose()
        {
            m_LastState?.Dispose();
            m_StateChanged = null;
        }

        protected void InvokeStateChanged()
        {
            PreStateChanged();
            m_StateChanged?.Invoke();
            PostStateChanged();
        }

        protected virtual void PreDispatchAction(IAction action)
        {
        }

        protected virtual void PreStateChanged()
        {
        }

        protected virtual void PostStateChanged()
        {
        }

        public State GetState()
        {
            return m_LastState;
        }

        public TState GetState<TState>() where TState : State
        {
            return m_LastState as TState;
        }
    }
}
