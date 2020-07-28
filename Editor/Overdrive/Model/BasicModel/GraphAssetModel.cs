using System;
using System.IO;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    public abstract class GraphAssetModel : ScriptableObject, IGTFGraphAssetModel
    {
        [SerializeReference]
        GraphModel m_GraphModel;

        public IGTFGraphModel GraphModel => m_GraphModel;

        public string Name
        {
            get => name;
            set => name = value;
        }

        protected abstract Type GraphModelType { get; }

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

        public void CreateGraph(string graphName, Type stencilType, bool writeOnDisk = true)
        {
            Debug.Assert(typeof(IGTFGraphModel).IsAssignableFrom(GraphModelType));
            var graphModel = (IGTFGraphModel)Activator.CreateInstance(GraphModelType);
            graphModel.Name = graphName;
            graphModel.AssetModel = this;
            m_GraphModel = graphModel as GraphModel;

            if (m_GraphModel == null)
                return;

            if (writeOnDisk)
            {
                EditorUtility.SetDirty(this);
            }

            Debug.Assert(typeof(Stencil).IsAssignableFrom(stencilType));
            var stencil = (Stencil)Activator.CreateInstance(stencilType);
            Assert.IsNotNull(stencil);
            m_GraphModel.Stencil = stencil;
            if (writeOnDisk)
                EditorUtility.SetDirty(this);
        }

        protected virtual void OnEnable()
        {
            m_GraphModel?.OnEnable();
        }

        protected virtual void OnDisable()
        {
        }

        public void Dispose() {}
    }
}
