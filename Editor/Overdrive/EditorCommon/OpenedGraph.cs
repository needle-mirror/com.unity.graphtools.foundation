using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    [Serializable]
    //[MovedFrom(false, "UnityEditor.VisualScripting.Editor", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    [MovedFrom(false, "UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting")]
    public struct OpenedGraph
    {
        public IGraphAssetModel GraphAssetModel;
        public GameObject BoundObject;

        public OpenedGraph(IGraphAssetModel graphAssetModel, GameObject boundObject)
        {
            GraphAssetModel = graphAssetModel;
            BoundObject = boundObject;
        }
    }
}
