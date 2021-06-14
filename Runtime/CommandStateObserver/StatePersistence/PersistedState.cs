using System;
using System.Collections.Generic;

namespace UnityEngine.GraphToolsFoundation.CommandStateObserver
{
    /// <summary>
    /// Used to persist the tool editor state to disk, in the k_StateCache folder.
    /// </summary>
    public sealed class PersistedState
    {
        static readonly StateCache k_StateCache = new StateCache("Library/StateCache/ToolState/");
        static readonly Dictionary<Hash128, List<Hash128>> k_ViewState = new Dictionary<Hash128, List<Hash128>>();

        // Hash base for view-only state components
        Hash128 m_ViewHashBase;
        // Hash base for asset and asset-view state components
        Hash128 m_AssetViewHashBase;

        /// <summary>
        /// Initializes a new instance of the <see cref="PersistedState" /> class.
        /// </summary>
        internal PersistedState()
        {
            m_ViewHashBase = new Hash128();
        }

        /// <summary>
        /// Sets the asset key for creating or retrieving state components.
        /// </summary>
        /// <param name="key">The unique asset key (for example, its GUID).</param>
        public void SetAssetKey(string key)
        {
            m_AssetViewHashBase = new Hash128();
            m_AssetViewHashBase.Append(key);
        }

        static Hash128 GetComponentStorageHash(Hash128 hashBase, Type componentType, Hash128 viewGUID)
        {
            var hash = hashBase;
            hash.Append(componentType?.FullName ?? "");
            hash.Append(viewGUID.ToString());
            return hash;
        }

        static void AddViewKey(Hash128 viewGUID, Hash128 key)
        {
            if (viewGUID != default)
            {
                if (!k_ViewState.ContainsKey(viewGUID))
                {
                    k_ViewState[viewGUID] = new List<Hash128>();
                }

                k_ViewState[viewGUID].Add(key);
            }
        }

        /// <summary>
        /// Gets a state component of type <typeparamref name="TComponent"/> associated to <paramref name="viewGUID"/>. If none exists, create a new one.
        /// </summary>
        /// <param name="viewGUID">The guid identifying the view.</param>
        /// <param name="name">The name of the new component.</param>
        /// <typeparam name="TComponent">The type of component to create.</typeparam>
        /// <returns>A state component of the requested type, loaded from the state cache or newly created.</returns>
        public TComponent GetOrCreateViewStateComponent<TComponent>(Hash128 viewGUID, string name)
            where TComponent : class, IViewStateComponent, new()
        {
            var componentKey = GetComponentStorageHash(m_ViewHashBase, typeof(TComponent), viewGUID);
            AddViewKey(viewGUID, componentKey);
            var component = k_StateCache.GetState(componentKey, () => new TComponent { ViewGUID = viewGUID });
            component.StateSlotName = name;
            return component;
        }

        /// <summary>
        /// Gets a state component of type <typeparamref name="TComponent"/> associated to the asset. If none exists, create a new one.
        /// </summary>
        /// <param name="name">The name of the new component.</param>
        /// <typeparam name="TComponent">The type of component to create.</typeparam>
        /// <returns>A state component of the requested type, loaded from the state cache or newly created.</returns>
        public TComponent GetOrCreateAssetStateComponent<TComponent>(string name)
            where TComponent : class, IAssetStateComponent, new()
        {
            var componentKey = GetComponentStorageHash(m_AssetViewHashBase, typeof(TComponent), default);
            var component = k_StateCache.GetState(componentKey, () => new TComponent());
            component.StateSlotName = name;
            return component;
        }

        /// <summary>
        /// Gets a state component of type <typeparamref name="TComponent"/> associated to <paramref name="viewGUID"/> and the asset.
        /// If none exists, create a new one.
        /// </summary>
        /// <param name="name">The name of the new component.</param>
        /// <param name="viewGUID">The guid identifying the view.</param>
        /// <typeparam name="TComponent">The type of component to create.</typeparam>
        /// <returns>A state component of the requested type, loaded from the state cache or newly created.</returns>
        public TComponent GetOrCreateAssetViewStateComponent<TComponent>(Hash128 viewGUID, string name)
            where TComponent : class, IAssetViewStateComponent, new()
        {
            var componentKey = GetComponentStorageHash(m_AssetViewHashBase, typeof(TComponent), viewGUID);
            AddViewKey(viewGUID, componentKey);
            var component = k_StateCache.GetState(componentKey, () => new TComponent { ViewGUID = viewGUID });
            component.StateSlotName = name;
            return component;
        }

        /// <summary>
        /// Replaces a view state component.
        /// </summary>
        /// <param name="viewGUID">The unique GUID of the view (window)</param>
        /// <param name="state">The new view state component.</param>
        /// <typeparam name="TComponent">Type of the view state component.</typeparam>
        public void SetViewStateComponent<TComponent>(Hash128 viewGUID, TComponent state)
            where TComponent : class, IViewStateComponent
        {
            var componentKey = GetComponentStorageHash(m_ViewHashBase, typeof(TComponent), viewGUID);
            AddViewKey(viewGUID, componentKey);
            k_StateCache.SetState(componentKey, state);
        }

        /// <summary>
        /// Replaces an asset state component.
        /// </summary>
        /// <param name="state">The new asset state component.</param>
        /// <typeparam name="TComponent">Type of the asset state component.</typeparam>
        public void SetAssetStateComponent<TComponent>(TComponent state)
            where TComponent : class, IStateComponent
        {
            var componentKey = GetComponentStorageHash(m_AssetViewHashBase, typeof(TComponent), default);
            k_StateCache.SetState(componentKey, state);
        }

        /// <summary>
        /// Replaces an asset-view state component.
        /// </summary>
        /// <param name="viewGUID">The unique GUID of the view (window)</param>
        /// <param name="state">The new asset-view state component.</param>
        /// <typeparam name="TComponent">Type of the asset-view state component.</typeparam>
        public void SetAssetViewStateComponent<TComponent>(Hash128 viewGUID, TComponent state)
            where TComponent : class, IAssetViewStateComponent
        {
            var componentKey = GetComponentStorageHash(m_AssetViewHashBase, typeof(TComponent), viewGUID);
            AddViewKey(viewGUID, componentKey);
            k_StateCache.SetState(componentKey, state);
        }

        /// <summary>
        /// Removes all state components associated with a view.
        /// </summary>
        /// <param name="viewGUID">The GUID of the view for which to remove associated components.</param>
        public static void RemoveViewState(Hash128 viewGUID)
        {
            if (k_ViewState.TryGetValue(viewGUID, out var hashList))
            {
                foreach (var hash in hashList)
                {
                    k_StateCache.RemoveState(hash);
                }

                k_ViewState.Remove(viewGUID);
            }
        }

        /// <summary>
        /// Writes all state components to disk.
        /// </summary>
        internal static void Flush()
        {
            k_StateCache.Flush();
        }
    }
}
