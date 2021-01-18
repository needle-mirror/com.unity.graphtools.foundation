using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEditor.PackageManager;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public sealed class PersistedEditorState
    {
        static readonly EditorStateCache k_StateCache = new EditorStateCache("Library/StateCache/GraphToolsFoundation/");
        static readonly Dictionary<GUID, List<Hash128>> k_ViewState = new Dictionary<GUID, List<Hash128>>();

        Hash128 m_ViewHashBase;
        Hash128 m_AssetViewHashBase;

        public PersistedEditorState(string assetModelKey)
        {
            // Hash base is empty for view-only state components
            m_ViewHashBase = new Hash128();

            // Hash base is assetModelKey for asset-view state components
            m_AssetViewHashBase = new Hash128();
            m_AssetViewHashBase.Append(assetModelKey);
        }

        static Hash128 GetComponentStorageHash(Hash128 hashBase, Type componentType, GUID viewGUID)
        {
            var hash = hashBase;
            hash.Append(componentType?.FullName ?? "");
            hash.Append(viewGUID.ToString());
            return hash;
        }

        static void AddViewKey(GUID viewGUID, Hash128 key)
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

        public T GetOrCreateViewStateComponent<T>(GUID viewGUID) where T : ViewStateComponent, new()
        {
            var componentKey = GetComponentStorageHash(m_ViewHashBase, typeof(T), viewGUID);
            AddViewKey(viewGUID, componentKey);
            return k_StateCache.GetState(componentKey, new T { ViewGUID = viewGUID });
        }

        public T GetOrCreateAssetStateComponent<T>() where T : EditorStateComponent, new()
        {
            var componentKey = GetComponentStorageHash(m_AssetViewHashBase, typeof(T), default);
            return k_StateCache.GetState(componentKey, new T());
        }

        public T GetOrCreateAssetViewStateComponent<T>(GUID viewGUID) where T : AssetViewStateComponent, new()
        {
            var componentKey = GetComponentStorageHash(m_AssetViewHashBase, typeof(T), viewGUID);
            AddViewKey(viewGUID, componentKey);
            return k_StateCache.GetState(componentKey, new T { ViewGUID = viewGUID });
        }

        public static void RemoveViewState(GUID viewGUID)
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

        public static void Flush()
        {
            k_StateCache.Flush();
        }
    }
}
