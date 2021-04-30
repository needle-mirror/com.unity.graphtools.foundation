using System;

namespace UnityEngine.GraphToolsFoundation.CommandStateObserver
{
    /// <summary>
    /// Base class for implementations of <see cref="IAssetViewStateComponent"/>.
    /// </summary>
    [Serializable]
    public abstract class AssetViewStateComponent<TUpdater> : StateComponent<TUpdater>, IAssetViewStateComponent
        where TUpdater : class, IStateComponentUpdater, new()
    {
        [SerializeField]
        Hash128 m_ViewGUID;

        /// <inheritdoc/>
        public Hash128 ViewGUID
        {
            get => m_ViewGUID;
            set => m_ViewGUID = value;
        }
    }
}
