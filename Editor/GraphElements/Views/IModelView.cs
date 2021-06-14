using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface for views.
    /// </summary>
    public interface IModelView
    {
        /// <summary>
        /// The command dispatcher.
        /// </summary>
        CommandDispatcher CommandDispatcher { get; }
    }
}
