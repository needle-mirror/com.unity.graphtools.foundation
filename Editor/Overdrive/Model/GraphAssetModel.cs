using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public abstract class GraphAssetModel : ScriptableObject, IGraphAssetModel, ISerializationCallbackReceiver
    {
        [SerializeReference]
        IGraphModel m_GraphModel;

        public IGraphModel GraphModel => m_GraphModel;
        public abstract IBlackboardGraphModel BlackboardGraphModel { get; }

        public string Name
        {
            get => name;
            set => name = value;
        }

        public string FriendlyScriptName => StringExtensions.CodifyString(Name);

        public abstract string SourceFilePath { get; }

        protected abstract Type GraphModelType { get; }

        public void CreateGraph(string graphName, Type stencilType = null, bool writeOnDisk = true)
        {
            Debug.Assert(typeof(IGraphModel).IsAssignableFrom(GraphModelType));
            var graphModel = (IGraphModel)Activator.CreateInstance(GraphModelType);
            if (graphModel == null)
                return;

            graphModel.StencilType = stencilType ?? graphModel.DefaultStencilType;

            graphModel.Name = graphName;
            graphModel.AssetModel = this;
            m_GraphModel = graphModel;

            if (writeOnDisk)
            {
                EditorUtility.SetDirty(this);
            }

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
        public void OnBeforeSerialize() {}

        public void OnAfterDeserialize()
        {
            GraphModel?.OnAfterDeserializeAssetModel();
        }
    }
}
