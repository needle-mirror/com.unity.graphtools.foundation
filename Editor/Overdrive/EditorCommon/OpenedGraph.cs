using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    [Serializable]
    //[MovedFrom(false, "UnityEditor.VisualScripting.Editor", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    [MovedFrom(false, "UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting")]
    public struct OpenedGraph
    {
        [SerializeField]
        string m_GraphAssetModelPath;

        [SerializeField]
        string m_GraphName;

        [SerializeField]
        GameObject m_BoundObject;

        IGraphAssetModel m_GraphAssetModel;

        public IGraphAssetModel GraphAssetModel
        {
            get
            {
                if (m_GraphAssetModel == null && !string.IsNullOrEmpty(m_GraphAssetModelPath))
                {
                    m_GraphAssetModel = AssetDatabase.LoadAssetAtPath(m_GraphAssetModelPath, typeof(Object)) as IGraphAssetModel;
                    m_GraphName = m_GraphAssetModel?.FriendlyScriptName;
                }
                return m_GraphAssetModel;
            }
        }

        public string GraphAssetModelPath => m_GraphAssetModelPath;

        public string GraphName => m_GraphName;

        public GameObject BoundObject => m_BoundObject;

        public OpenedGraph(IGraphAssetModel graphAssetModel, GameObject boundObject)
        {
            m_GraphAssetModel = graphAssetModel;
            m_GraphAssetModelPath = graphAssetModel == null ? null : AssetDatabase.GetAssetPath(graphAssetModel as Object);
            m_GraphName = graphAssetModel?.FriendlyScriptName;
            m_BoundObject = boundObject;
        }
    }
}
