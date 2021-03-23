using System;
using System.Collections.Generic;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface for state observers.
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
        /// Gets the last observed component version of type <paramref name="componentName"/>.
        /// </summary>
        /// <param name="componentName">The name of the state component for which to get the last observed version.</param>
        /// <returns>Returns the last observed component version of type <paramref name="componentName"/>.</returns>
        StateComponentVersion GetLastObservedComponentVersion(string componentName);

        /// <summary>
        /// Updates the observed version for component type <paramref name="componentName"/> to <paramref name="newVersion"/>.
        /// </summary>
        /// <param name="componentName">The name of the state component for which to update the version.</param>
        /// <param name="newVersion">The new version.</param>
        void UpdateObservedVersion(string componentName, StateComponentVersion newVersion);

        /// <summary>
        /// Observes the <see cref="ObservedStateComponents"/> and modifies the <see cref="ModifiedStateComponents"/>.
        /// </summary>
        /// <param name="state">The state to observe.</param>
        void Observe(GraphToolState state);
    }

    static class StateObserverHelper
    {
        internal static IStateObserver CurrentObserver { get; set; }
    }
}
