using System;

namespace UnityEngine.GraphToolsFoundation.CommandStateObserver
{
    interface IInternalStateObserver
    {
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
    }

    static class StateObserverHelper
    {
        internal static IStateObserver CurrentObserver { get; set; }
    }
}
