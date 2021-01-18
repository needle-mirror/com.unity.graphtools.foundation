using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public static class GraphAssetModelExtensions
    {
        public static string GetPath(this IGraphAssetModel self)
        {
            var obj = self as Object;
            return obj ? AssetDatabase.GetAssetPath(obj) : "";
        }
    }
}
