using System;

namespace UnityEngine.GraphToolsFoundation.CommandStateObserver
{
    /// <summary>
    /// Base class for implementations of <see cref="IAssetStateComponent"/>.
    /// </summary>
    [Serializable]
    public abstract class AssetStateComponent<TUpdater> : StateComponent<TUpdater>, IAssetStateComponent
        where TUpdater : class, IStateComponentUpdater, new()
    {
    }
}
