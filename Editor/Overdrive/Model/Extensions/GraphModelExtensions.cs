using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Model
{
    public static class GraphModelExtensions
    {
        public static IGTFEdgePortalModel CreateOppositePortal(this IGTFGraphModel graphModel, IGTFEdgePortalModel edgePortalModel, Vector2 position = default, SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            return graphModel.CreateOppositePortal(edgePortalModel, position, spawnFlags);
        }

        public static TNodeType CreateNode<TNodeType>(this IGTFGraphModel graphModel) where TNodeType : class, IGTFNodeModel
        {
            return graphModel.CreateNode<TNodeType>("", default, SpawnFlags.Default);
        }

        public static IEnumerable<IHasDeclarationModel> FindReferencesInGraph(this IGTFGraphModel graphModel, IDeclarationModel variableDeclarationModel)
        {
            return graphModel.NodeModels.OfType<IHasDeclarationModel>().Where(v => v.DeclarationModel != null && variableDeclarationModel.Guid == v.DeclarationModel.Guid);
        }

        public static IEnumerable<T> FindReferencesInGraph<T>(this IGTFGraphModel graphModel, IDeclarationModel variableDeclarationModel) where T : IHasDeclarationModel
        {
            return graphModel.FindReferencesInGraph(variableDeclarationModel).OfType<T>();
        }

        public static bool HasAnyTopologyChange(this IGTFGraphModel graphModel)
        {
            return graphModel?.LastChanges != null && graphModel.LastChanges.HasAnyTopologyChange();
        }

        public static IEnumerable<IGTFPortModel> GetPortModels(this IGTFGraphModel graphModel)
        {
            return graphModel.NodeModels.OfType<IPortNode>().SelectMany(nodeModel => nodeModel.Ports);
        }
    }
}
