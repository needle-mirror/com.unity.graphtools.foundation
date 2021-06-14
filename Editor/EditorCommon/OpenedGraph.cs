using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Holds information about a graph displayed in the graph view editor window.
    /// </summary>
    [Serializable]
    public struct OpenedGraph
    {
        [SerializeField]
        string m_AssetGUID;

        [SerializeField]
        int m_GraphAssetObjectInstanceID;

        [SerializeField]
        long m_AssetLocalId;

        [SerializeField]
        GameObject m_BoundObject;

        // For tests
        internal IGraphAssetModel m_GraphAssetModel;

        /// <summary>
        /// Gets the graph asset model.
        /// </summary>
        /// <returns>The graph asset model.</returns>
        public IGraphAssetModel GetGraphAssetModel()
        {
            EnsureGraphAssetModelIsLoaded();
            return m_GraphAssetModel;
        }

        /// <summary>
        /// Gets the path of the graph asset model file on disk.
        /// </summary>
        /// <returns>The path of the graph asset file.</returns>
        public string GetGraphAssetModelPath()
        {
            EnsureGraphAssetModelIsLoaded();
            return m_GraphAssetModel == null ? null : AssetDatabase.GetAssetPath(m_GraphAssetModel as Object);
        }

        /// <summary>
        /// The GUID of the graph asset.
        /// </summary>
        public string GraphModelAssetGUID => m_AssetGUID;

        /// <summary>
        /// The GameObject bound to this graph.
        /// </summary>
        public GameObject BoundObject => m_BoundObject;

        /// <summary>
        /// The file id of the graph asset in the asset file.
        /// </summary>
        public long AssetLocalId => m_AssetLocalId;

        /// <summary>
        /// Checks whether this instance holds a valid graph asset model.
        /// </summary>
        /// <returns>True if the graph asset model is valid, false otherwise.</returns>
        public bool IsValid()
        {
            return GetGraphAssetModel() != null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenedGraph" /> class.
        /// </summary>
        /// <param name="graphAssetModel">The graph asset model.</param>
        /// <param name="boundObject">The GameObject bound to the graph.</param>
        public OpenedGraph(IGraphAssetModel graphAssetModel, GameObject boundObject)
        {
            if (graphAssetModel == null ||
                !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(graphAssetModel as Object, out m_AssetGUID, out m_AssetLocalId))
            {
                m_AssetGUID = "";
                m_AssetLocalId = 0L;
            }

            m_GraphAssetModel = graphAssetModel;
            m_GraphAssetObjectInstanceID = (graphAssetModel as Object)?.GetInstanceID() ?? 0;
            m_BoundObject = boundObject;
        }

        void EnsureGraphAssetModelIsLoaded()
        {
            // GUIDToAssetPath cannot be done in ISerializationCallbackReceiver.OnAfterDeserialize so we do it here.

            var doReload = false;
            if (m_GraphAssetModel == null)
            {
                doReload = true;
            }
            else if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(m_GraphAssetModel as Object, out var assetGuid, out long localId))
            {
                doReload = assetGuid != m_AssetGUID || localId != m_AssetLocalId;
            }

            if (doReload)
            {
                // Try to load object from its GUID. Will fail if it is a memory based asset or if the asset was deleted.
                if (!string.IsNullOrEmpty(m_AssetGUID))
                {
                    var graphAssetModelPath = AssetDatabase.GUIDToAssetPath(m_AssetGUID);
                    m_GraphAssetModel = Load(graphAssetModelPath, m_AssetLocalId);
                }

                // If it failed, try to retrieve object from its instance id (memory based asset).
                if (m_GraphAssetModel == null && m_GraphAssetObjectInstanceID != 0)
                {
                    m_GraphAssetModel = EditorUtility.InstanceIDToObject(m_GraphAssetObjectInstanceID) as IGraphAssetModel;
                }

                if (m_GraphAssetModel == null)
                {
                    m_AssetGUID = null;
                    m_AssetLocalId = 0;
                    m_GraphAssetObjectInstanceID = 0;
                }
                else
                {
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(m_GraphAssetModel as Object, out m_AssetGUID, out m_AssetLocalId);
                    m_GraphAssetObjectInstanceID = (m_GraphAssetModel as Object)?.GetInstanceID() ?? 0;
                }
            }
        }

        /// <summary>
        /// Loads a graph asset from file.
        /// </summary>
        /// <param name="path">The path of the file.</param>
        /// <param name="localFileId">The id of the asset in the file. If 0, the first asset of type <see cref="GraphAssetModel"/> will be loaded.</param>
        /// <returns>The loaded asset, or null if it was not found or if the asset found is not an <see cref="IGraphAssetModel"/>.</returns>
        public static IGraphAssetModel Load(string path, long localFileId)
        {
            IGraphAssetModel assetModel = null;

            if (localFileId != 0L)
            {
                var assets = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (var a in assets)
                {
                    if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(a, out _, out long localId))
                        continue;

                    if (localId == localFileId)
                    {
                        return a as IGraphAssetModel;
                    }
                }
            }
            else
            {
                assetModel = (IGraphAssetModel)AssetDatabase.LoadAssetAtPath(path, typeof(GraphAssetModel));
            }

            return assetModel;
        }
    }
}
