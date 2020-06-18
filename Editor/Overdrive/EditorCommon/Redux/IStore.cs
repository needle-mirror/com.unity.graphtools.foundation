using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface IStore
    {
        void Dispatch<TAction>(TAction action) where TAction : IAction;
    }

    public interface IStore<TState> : IStore
    {
        TState GetState();

        event Action StateChanged;

        void Register<TAction>(Reducer<TState, TAction> reducer) where TAction : IAction;
        void Unregister<TAction>() where TAction : IAction;

        void Register(Action<IAction> observer);
        void Unregister(Action<IAction> observer);
    }
}
