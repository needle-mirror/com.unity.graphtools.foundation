using System;

namespace UnityEngine.GraphToolsFoundation.CommandStateObserver
{
    /// <summary>
    /// Interface for state components that store data associated with an asset.
    /// </summary>
    /// <remarks>
    /// An IAssetStateComponent is state information that is tied to an asset only and will apply to any view.
    /// Example: the dirty state of the asset. When the asset is dirtied, we want to refresh all views
    /// that display the asset.
    /// </remarks>
    public interface IAssetStateComponent : IStateComponent
    {
    }
}
