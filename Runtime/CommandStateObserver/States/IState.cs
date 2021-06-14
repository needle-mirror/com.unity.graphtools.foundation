using System;
using System.Collections.Generic;

namespace UnityEngine.GraphToolsFoundation.CommandStateObserver
{
    /// <summary>
    /// Interface for state.
    /// </summary>
    public interface IState
    {
        /// <summary>
        /// Registers the default command handlers fot the state.
        /// </summary>
        /// <param name="dispatcher">The dispatcher to register the command handler to.</param>
        void RegisterCommandHandlers(Dispatcher dispatcher);

        /// <summary>
        /// All the state components.
        /// </summary>
        IEnumerable<IStateComponent> AllStateComponents { get; }
    }
}
