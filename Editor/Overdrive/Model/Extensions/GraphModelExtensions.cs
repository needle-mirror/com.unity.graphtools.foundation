using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public static class GraphModelExtensions
    {
        public static IEdgePortalModel CreateOppositePortal(this IGraphModel graphModel, IEdgePortalModel edgePortalModel, Vector2 position = default, SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            return graphModel.CreateOppositePortal(edgePortalModel, position, spawnFlags);
        }

        public static TNodeType CreateNode<TNodeType>(this IGraphModel graphModel) where TNodeType : class, INodeModel
        {
            return graphModel.CreateNode<TNodeType>("", default, SpawnFlags.Default);
        }

        public static IEnumerable<IHasDeclarationModel> FindReferencesInGraph(this IGraphModel graphModel, IDeclarationModel variableDeclarationModel)
        {
            return graphModel.NodeModels.OfType<IHasDeclarationModel>().Where(v => v.DeclarationModel != null && variableDeclarationModel.Guid == v.DeclarationModel.Guid);
        }

        public static IEnumerable<T> FindReferencesInGraph<T>(this IGraphModel graphModel, IDeclarationModel variableDeclarationModel) where T : IHasDeclarationModel
        {
            return graphModel.FindReferencesInGraph(variableDeclarationModel).OfType<T>();
        }

        public static bool HasAnyTopologyChange(this IGraphModel graphModel)
        {
            return graphModel?.LastChanges != null && graphModel.LastChanges.HasAnyTopologyChange();
        }

        public static IEnumerable<IPortModel> GetPortModels(this IGraphModel graphModel)
        {
            return graphModel.NodeModels.OfType<IPortNode>().SelectMany(nodeModel => nodeModel.Ports);
        }
    }
}
