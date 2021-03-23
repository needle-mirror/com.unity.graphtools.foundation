using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface for state component updaters.
    /// </summary>
    public interface IStateComponentUpdater
    {
        /// <summary>
        /// The state component to update.
        /// </summary>
        IStateComponent StateComponent { set; }

        /// <summary>
        /// Begins a modification of the state component.
        /// </summary>
        /// <remarks>
        /// Call this method before any other method of the updater.
        /// </remarks>
        void BeginStateChange();

        /// <summary>
        /// Ends a modification of the state component.
        /// </summary>
        /// <remarks>
        /// Call this method when you are done modifying the state component.
        /// </remarks>
        void EndStateChange();
    }
}
