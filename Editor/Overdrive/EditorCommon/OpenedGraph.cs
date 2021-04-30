using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Holds information about a graph displayed in the graph view editor window.
    /// </summary>
    [Serializable]
    //[MovedFrom(false, "UnityEditor.VisualScripting.Editor", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    [MovedFrom(false, "UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting")]
    public struct OpenedGraph
    {
        [SerializeField]
        string m_AssetGUID;

        [SerializeField]
        int m_GraphAssetInstanceID;

        [SerializeField]
        long m_FileId;

        [SerializeField]
        GameObject m_BoundObject;

        IGraphAssetModel m_GraphAssetModel;

        /// <summary>
        /// Gets the graph asset model.
        /// </summary>
        /// <returns>The graph asset model.</returns>
        public IGraphAssetModel GetGraphAssetModel()
        {
            EnsureGraphAssetModelIsLoaded();
            return m_GraphAssetModel;
        }

        [Obsolete("Use GetGraphAssetModel() instead. Added in 0.9+. (UnityUpgradable) -> GetGraphAssetModel() ")]
        public IGraphAssetModel GraphAssetModel => GetGraphAssetModel();

        /// <summary>
        /// Gets the path of the graph asset model file on disk.
        /// </summary>
        /// <returns>The path of the graph asset file.</returns>
        public string GetGraphAssetModelPath()
        {
            EnsureGraphAssetModelIsLoaded();
            return m_GraphAssetModel == null ? null : AssetDatabase.GetAssetPath(m_GraphAssetModel as Object);
        }

        [Obsolete("Use GetGraphAssetModelPath() instead. Added in 0.9+. (UnityUpgradable) -> GetGraphAssetModelPath() ")]
        public string GraphAssetModelPath => GetGraphAssetModelPath();

        [Obsolete("Use GetGraphAssetModel()?.FriendlyScriptName instead. Added in 0.9+.")]
        public string GraphName => GetGraphAssetModel()?.FriendlyScriptName;

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
        public long FileId => m_FileId;

        /// <summary>
        /// Checks whether this instance holds a valid graph asset model.
        /// </summary>
        /// <returns>True if the graph asset model is valid, false otherwise.</returns>
        public bool IsValid()
        {
            return GetGraphAssetModel() != null;
        }

        /// <summary>
        /// Initializes a new instance of the OpenedGraph class.
        /// </summary>
        /// <param name="graphAssetModel">The graph asset model.</param>
        /// <param name="boundObject">The GameObject bound to the graph.</param>
        public OpenedGraph(IGraphAssetModel graphAssetModel, GameObject boundObject)
        {
            if (graphAssetModel == null ||
                !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(graphAssetModel as Object, out m_AssetGUID, out m_FileId))
            {
                m_AssetGUID = "";
                m_FileId = 0L;
            }

            m_GraphAssetModel = graphAssetModel;
            m_GraphAssetInstanceID = (graphAssetModel as Object)?.GetInstanceID() ?? 0;
            m_BoundObject = boundObject;
        }

        void EnsureGraphAssetModelIsLoaded()
        {
            // GUIDToAssetPath cannot be done in ISerializationCallbackReceiver.OnAfterDeserialize so we do it here.

            // Try to load object from its GUID. Will fail if it is a memory based asset or if the asset was deleted.
            if (!string.IsNullOrEmpty(m_AssetGUID))
            {
                var graphAssetModelPath = AssetDatabase.GUIDToAssetPath(m_AssetGUID);
                m_GraphAssetModel = AssetDatabase.LoadAssetAtPath(graphAssetModelPath, typeof(Object)) as IGraphAssetModel;

                // Update the instance ID
                m_GraphAssetInstanceID = (m_GraphAssetModel as Object)?.GetInstanceID() ?? 0;
            }

            // If it failed, try to retrieve object from its instance id (memory based asset).
            if (m_GraphAssetModel == null && m_GraphAssetInstanceID != 0)
            {
                m_GraphAssetModel = EditorUtility.InstanceIDToObject(m_GraphAssetInstanceID) as IGraphAssetModel;
            }
        }
    }
}
