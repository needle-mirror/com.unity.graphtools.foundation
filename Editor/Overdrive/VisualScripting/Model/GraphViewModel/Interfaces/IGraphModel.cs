using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel
{
    public interface IGraphModel
    {
        string Name { get; }
        IGraphAssetModel AssetModel { get; }
        IReadOnlyList<INodeModel> NodeModels { get; }
        IReadOnlyList<IEdgeModel> EdgeModels { get; }
        IReadOnlyList<IStickyNoteModel> StickyNoteModels { get; }
        IReadOnlyList<IPlacematModel> PlacematModels { get; }
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
        void MoveEdgeBefore(IEdgeModel toMove, IEdgeModel reference);
        void MoveEdgeAfter(IEdgeModel toMove, IEdgeModel reference);
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
