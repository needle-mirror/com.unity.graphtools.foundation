using System;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class AssetModificationWatcher : AssetModificationProcessor
    {
        public static int Version;

        static string[] OnWillSaveAssets(string[] paths)
        {
            if (paths.Any(AssetWatcher.AssetAtPathIsGraphAsset))
                Version++;

            return paths;
        }

        static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions options)
        {
            if (AssetWatcher.AssetAtPathIsGraphAsset(assetPath))
                Version++;
            return AssetDeleteResult.DidNotDelete;
        }

        static AssetMoveResult OnWillMoveAsset(string sourcePath, string destinationPath)
        {
            if (AssetWatcher.AssetAtPathIsGraphAsset(sourcePath))
                Version++;
            return AssetMoveResult.DidNotMove;
        }
    }
}
