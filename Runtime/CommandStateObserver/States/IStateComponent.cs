using System;

namespace UnityEngine.GraphToolsFoundation.CommandStateObserver
{
    /// <summary>
    /// Interface for state components.
    /// </summary>
    public interface IStateComponent : IDisposable
    {
        /// <summary>
        /// The slot name of this component in the <see cref="State"/>.
        /// </summary>
        string StateSlotName { get; set; }

        /// <summary>
        /// The current version of the state component.
        /// </summary>
        uint CurrentVersion { get; }

        /// <summary>
        /// Called just before serialization of state component to disk.
        /// </summary>
        void BeforeSerialize();

        /// <summary>
        /// Called immediately after deserialization of state component from disk.
        /// </summary>
        void AfterDeserialize();

        /// <summary>
        /// Checks if the state component has any available changesets.
        /// </summary>
        /// <returns>Returns true if the state component has any available changesets.</returns>
        bool HasChanges();

        /// <summary>
        /// Removes changesets that register changes that have occured up to and including <paramref name="untilVersion"/>
        /// </summary>
        /// <param name="untilVersion">The version up until which the changesets should be purged.</param>
        void PurgeOldChangesets(uint untilVersion);

        /// <summary>
        /// Gets the type of update an observer should do.
        /// </summary>
        /// <param name="observerVersion">The last state component observed by the observer.</param>
        /// <returns>Returns the type of update an observer should do.</returns>
        UpdateType GetUpdateType(StateComponentVersion observerVersion);
    }
}
