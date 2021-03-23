using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Base class for implementations of <see cref="IAssetViewStateComponent"/>.
    /// </summary>
    [Serializable]
    public abstract class AssetViewStateComponent<TUpdater> : StateComponent<TUpdater>, IAssetViewStateComponent
        where TUpdater : class, IStateComponentUpdater, new()
    {
        [SerializeField]
        SerializableGUID m_ViewGUID;

        /// <inheritdoc/>
        public SerializableGUID ViewGUID
        {
            get => m_ViewGUID;
            set => m_ViewGUID = value;
        }
    }
}
