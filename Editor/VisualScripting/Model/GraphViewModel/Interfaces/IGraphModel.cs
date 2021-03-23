using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;

namespace UnityEditor.VisualScripting.GraphViewModel
{
    public interface IGraphModel : IDisposable
    {
        string Name { get; }
        IGraphAssetModel AssetModel { get; }
        IReadOnlyList<INodeModel> NodeModels { get; }
        IReadOnlyList<IEdgeModel> EdgeModels { get; }
        string GetAssetPath();
        IEnumerable<IEdgeModel> GetEdgesConnections(IPortModel portModel);
        IEnumerable<IPortModel> GetConnections(IPortModel portModel);
        Stencil Stencil { get; }
        string FriendlyScriptName { get; }
        IGraphChangeList LastChanges { get; }
        ModelState State { get; }
        IReadOnlyDictionary<GUID, INodeModel> NodesByGuid { get; }
        void ResetChanges();
        void CleanUp();
        string GetUniqueName(string baseName);
        TNodeType CreateNode<TNodeType>(string nodeName = "", Vector2 position = default, SpawnFlags spawnFlags = SpawnFlags.Default, Action<TNodeType> preDefineSetup = null, GUID? guid = null) where TNodeType : NodeModel;
        INodeModel CreateNode(Type nodeTypeToCreate, string nodeName, Vector2 position, SpawnFlags spawnFlags = SpawnFlags.Default, Action<NodeModel> preDefineSetup = null, GUID? guid = null);
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class IGraphModelExtensions
    {
        public static bool HasAnyTopologyChange(this IGraphModel graph)
        {
            return graph?.LastChanges != null && graph.LastChanges.HasAnyTopologyChange();
        }
    }
}
