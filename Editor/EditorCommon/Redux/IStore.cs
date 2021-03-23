using System;

namespace UnityEditor.EditorCommon.Redux
{
    interface IStore<TState>
    {
        void Dispatch<TAction>(TAction action) where TAction : IAction;

        TState GetState();

        event Action StateChanged;

        void Register<TAction>(Reducer<TState, TAction> reducer) where TAction : IAction;
        void Unregister<TAction>() where TAction : IAction;

        void Register(Action<IAction> observer);
        void Unregister(Action<IAction> observer);
    }
}
