using System;
using System.IO;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface IGraphAssetModel
    {
        string Name { get; set; }
        string FriendlyScriptName { get; }
        IGraphModel GraphModel { get; }

        /// <summary>
        /// The dirty state of the asset (true if it needs to be saved)
        /// </summary>
        bool Dirty { get; set; }
        void CreateGraph(string graphName, Type stencilType = null, bool writeOnDisk = true);
    }

    // ReSharper disable once InconsistentNaming
    public static class IGraphAssetModelHelper
    {
        public static AssetT Create<AssetT>(string assetName, string assetPath, bool writeOnDisk = true) where AssetT : class, IGraphAssetModel
        {
            return (AssetT)Create(assetName, assetPath, typeof(AssetT), writeOnDisk);
        }

        public static IGraphAssetModel Create(string assetName, string assetPath, Type assetTypeToCreate, bool writeOnDisk = true)
        {
            var asset = ScriptableObject.CreateInstance(assetTypeToCreate);
            if (!string.IsNullOrEmpty(assetPath) && writeOnDisk)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(assetPath) ?? "");
                if (File.Exists(assetPath))
                    AssetDatabase.DeleteAsset(assetPath);
                AssetDatabase.CreateAsset(asset, assetPath);
            }
            asset.name = assetName;
            return asset as IGraphAssetModel;
        }
    }
}
