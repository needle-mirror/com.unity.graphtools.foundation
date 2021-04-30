namespace UnityEngine.GraphToolsFoundation.CommandStateObserver
{
    /// <summary>
    /// Interface for state components that store data associated with an asset and a view.
    /// </summary>
    /// <remarks>
    /// IAssetViewStateComponent are tied to both a view and an asset. Example: selection state,
    /// which is tied to an asset in the context of a single view. The same asset in a different view
    /// would have a different selection state.
    /// </remarks>
    public interface IAssetViewStateComponent : IStateComponent
    {
        /// <summary>
        /// The unique ID of the referenced view.
        /// </summary>
        Hash128 ViewGUID { get; set; }
    }
}
