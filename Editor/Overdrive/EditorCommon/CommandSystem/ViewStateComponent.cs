using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Base class for implementations of <see cref="IViewStateComponent"/>.
    /// </summary>
    [Serializable]
    public abstract class ViewStateComponent<TUpdater> : StateComponent<TUpdater>, IViewStateComponent
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
