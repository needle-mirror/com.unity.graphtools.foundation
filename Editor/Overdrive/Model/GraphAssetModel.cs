using System;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public abstract class GraphAssetModel : ScriptableObject, IGraphAssetModel
    {
        [SerializeReference]
        IGraphModel m_GraphModel;

        public bool Dirty
        {
            get;
            set;
        }

        public IGraphModel GraphModel => m_GraphModel;

        public string Name
        {
            get => name;
            set => name = value;
        }

        public string FriendlyScriptName => Name.CodifyString();

        protected abstract Type GraphModelType { get; }

        public void CreateGraph(string graphName, Type stencilType = null, bool writeOnDisk = true)
        {
            Debug.Assert(typeof(IGraphModel).IsAssignableFrom(GraphModelType));
            var graphModel = (IGraphModel)Activator.CreateInstance(GraphModelType);
            if (graphModel == null)
                return;

            graphModel.StencilType = stencilType ?? graphModel.DefaultStencilType;

            graphModel.AssetModel = this;
            m_GraphModel = graphModel;

            if (writeOnDisk)
            {
                EditorUtility.SetDirty(this);
            }
        }

        protected virtual void OnEnable()
        {
            m_GraphModel?.OnEnable();
        }

        protected virtual void OnDisable()
        {
            m_GraphModel?.OnDisable();
        }

    }
}
