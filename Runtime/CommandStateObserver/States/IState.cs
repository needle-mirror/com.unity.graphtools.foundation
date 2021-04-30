using System;
using System.Collections.Generic;

namespace UnityEngine.GraphToolsFoundation.CommandStateObserver
{
    /// <summary>
    /// Interface for state.
    /// </summary>
    public interface IState
    {
        void RegisterCommandHandlers(Dispatcher dispatcher);

        /// <summary>
        /// All the state components.
        /// </summary>
        IEnumerable<IStateComponent> AllStateComponents { get; }
    }
}
