using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public delegate TState Reducer<TState, in TAction>(TState previousState, TAction action) where TAction : IAction;
}
