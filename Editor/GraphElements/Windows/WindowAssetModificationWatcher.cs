using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    class WindowAssetModificationWatcher : AssetModificationProcessor
    {
        static bool AssetAtPathIsGraphAsset(string path)
        {
            return typeof(IGraphAssetModel).IsAssignableFrom(AssetDatabase.GetMainAssetTypeAtPath(path));
        }

        static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions options)
        {
            if (AssetAtPathIsGraphAsset(assetPath))
            {
                var guid = AssetDatabase.AssetPathToGUID(assetPath);
                var windows = Resources.FindObjectsOfTypeAll<GraphViewEditorWindow>();
                foreach (var window in windows)
                {
                    if (window.CommandDispatcher.State.WindowState.CurrentGraph.GraphModelAssetGUID == guid)
                    {
                        window.CommandDispatcher.State.LoadGraphAsset(null, null);
                    }
                }
            }
            return AssetDeleteResult.DidNotDelete;
        }

        static AssetMoveResult OnWillMoveAsset(string sourcePath, string destinationPath)
        {
            if (AssetAtPathIsGraphAsset(sourcePath))
            {
                var guid = AssetDatabase.AssetPathToGUID(sourcePath);
                var windows = Resources.FindObjectsOfTypeAll<GraphViewEditorWindow>();
                foreach (var window in windows)
                {
                    window.CommandDispatcher.State.GraphAssetChanged(guid);
                }
            }

            return AssetMoveResult.DidNotMove;
        }
    }
}
