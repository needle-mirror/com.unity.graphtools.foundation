using System;
using System.IO;
using JetBrains.Annotations;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel
{
    public abstract class GraphAssetModel : ScriptableObject, IGraphAssetModel
    {
        [SerializeReference]
        GraphModel m_GraphModel;

        public string Name => name;
        IGTFGraphModel IGTFGraphAssetModel.GraphModel => m_GraphModel;
        public IGraphModel GraphModel => m_GraphModel;

        public static GraphAssetModel Create(string assetName, string assetPath, Type assetTypeToCreate, bool writeOnDisk = true)
        {
            var asset = (GraphAssetModel)CreateInstance(assetTypeToCreate);
            if (!string.IsNullOrEmpty(assetPath) && writeOnDisk)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(assetPath) ?? "");
                if (File.Exists(assetPath))
                    AssetDatabase.DeleteAsset(assetPath);
                AssetDatabase.CreateAsset(asset, assetPath);
            }
            asset.name = assetName;
            return asset;
        }

        [PublicAPI]
        public TGraphType CreateGraph<TGraphType>(string actionName, Type stencilType, bool writeOnDisk = true)
            where TGraphType : GraphModel
        {
            return (TGraphType)CreateGraph(typeof(TGraphType), actionName, stencilType, writeOnDisk);
        }

        [PublicAPI]
        public GraphModel CreateGraph(Type graphTypeToCreate, string graphName, Type stencilType, bool writeOnDisk = true)
        {
            var graphModel = (GraphModel)Activator.CreateInstance(graphTypeToCreate);
            graphModel.name = graphName;
            graphModel.AssetModel = this;
            m_GraphModel = graphModel;
            if (writeOnDisk)
                this.SetAssetDirty();
            var stencil = (Stencil)Activator.CreateInstance(stencilType);
            Assert.IsNotNull(stencil);
            graphModel.Stencil = stencil;
            if (writeOnDisk)
                EditorUtility.SetDirty(this);
            return graphModel;
        }

        void OnEnable()
        {
            m_GraphModel?.OnEnable();
        }

        public bool IsSameAsset(IGraphAssetModel otherGraphAssetModel)
        {
            return GetHashCode() == otherGraphAssetModel?.GetHashCode();
        }

        public void ShowInInspector()
        {
            Selection.activeObject = this;
        }

        public void Dispose() {}
    }

    public static class GraphAssetModelExtensions
    {
        public static void SetAssetDirty(this IGraphAssetModel graphAssetModel)
        {
            if (graphAssetModel as Object)
                EditorUtility.SetDirty((Object)graphAssetModel);
        }
    }
}
