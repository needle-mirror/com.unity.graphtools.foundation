using System;

namespace UnityEngine.GraphToolsFoundation.CommandStateObserver
{
    /// <summary>
    /// Interface for state component updaters.
    /// </summary>
    public interface IStateComponentUpdater : IDisposable
    {
        /// <summary>
        /// Initialize the updater with the state to update.
        /// </summary>
        /// <param name="state">The state to update.</param>
        void Initialize(IStateComponent state);
    }
}
