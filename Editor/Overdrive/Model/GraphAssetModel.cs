using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public abstract class GraphAssetModel : ScriptableObject, IGraphAssetModel
    {
        [SerializeReference]
        IGraphModel m_GraphModel;

        public IGraphModel GraphModel => m_GraphModel;

        public string Name
        {
            get => name;
            set => name = value;
        }

        protected abstract Type GraphModelType { get; }

        public void CreateGraph(string graphName, Type stencilType, bool writeOnDisk = true)
        {
            Debug.Assert(typeof(IGraphModel).IsAssignableFrom(GraphModelType));
            var graphModel = (IGraphModel)Activator.CreateInstance(GraphModelType);
            graphModel.Name = graphName;
            graphModel.AssetModel = this;
            m_GraphModel = graphModel;

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
            m_GraphModel?.OnDisable();
        }

        public void Dispose() {}
    }
}
