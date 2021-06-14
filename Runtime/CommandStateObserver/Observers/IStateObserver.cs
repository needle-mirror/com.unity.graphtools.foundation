using System;
using System.Collections.Generic;

namespace UnityEngine.GraphToolsFoundation.CommandStateObserver
{
    /// <summary>
    /// Base interface for state observers.
    /// </summary>
    public interface IStateObserver
    {
        /// <summary>
        /// The state components observed by the observer.
        /// </summary>
        IEnumerable<string> ObservedStateComponents { get; }

        /// <summary>
        /// The state components modified by the observer.
        /// </summary>
        IEnumerable<string> ModifiedStateComponents { get; }

        /// <summary>
        /// Observes the <see cref="IStateObserver.ObservedStateComponents"/> and modifies the <see cref="IStateObserver.ModifiedStateComponents"/>.
        /// </summary>
        /// <param name="state">The state to observe.</param>
        void Observe(IState state);
    }
}
