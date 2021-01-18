using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Spawn flags dictates multiple operations during the NodeModels creation.
    /// </summary>
    [Flags]
    public enum SpawnFlags
    {
        None   = 0,
        Reserved0  = 1 << 0,
        Reserved1 = 1 << 1,
        /// <summary>
        /// The created NodeModel is not added to a Stack/Graph. Useful for display only purposes.
        /// </summary>
        Orphan  = 1 << 2,
        /// <summary>
        /// This include the SpawnFlags.Orphan and SpawnFlags.CreateNodeAsset
        /// </summary>
        Default = None,
    }

    public static class SpawnFlagsExtensions
    {
        public static bool IsOrphan(this SpawnFlags f) => (f & SpawnFlags.Orphan) != 0;
    }

    public interface IGraphModel : IDisposable
    {
        Stencil Stencil { get; }
        Type DefaultStencilType { get; }
        Type StencilType { get; set; }
        IGraphAssetModel AssetModel { get; set; }

        void OnEnable();
        void OnDisable();
        void OnAfterDeserializeAssetModel();

        string Name { get; set; }

        IReadOnlyList<INodeModel> NodeModels { get; }
        IReadOnlyList<IEdgeModel> EdgeModels { get; }
        IReadOnlyList<IBadgeModel> BadgeModels { get; }
        IReadOnlyList<IStickyNoteModel> StickyNoteModels { get; }
        IReadOnlyList<IPlacematModel> PlacematModels { get; }
        IReadOnlyList<IVariableDeclarationModel> VariableDeclarations { get; }
        IReadOnlyList<IDeclarationModel> PortalDeclarations { get; }


        IVariableDeclarationModel CreateGraphVariableDeclaration(TypeHandle variableDataType,
            string variableName, ModifierFlags modifierFlags, bool isExposed, IConstant initializationModel = null, GUID? guid = null);
        IReadOnlyCollection<IGraphElementModel> DeleteVariableDeclarations(IReadOnlyCollection<IVariableDeclarationModel> variableModels, bool deleteUsages);
        IVariableDeclarationModel DuplicateGraphVariableDeclaration(IVariableDeclarationModel sourceModel);

        IDeclarationModel CreateGraphPortalDeclaration(string portalName, SpawnFlags spawnFlags = SpawnFlags.Default);
        IEdgePortalModel CreateOppositePortal(IEdgePortalModel edgePortalModel, Vector2 position, SpawnFlags spawnFlags = SpawnFlags.Default);
        IEdgePortalEntryModel CreateEntryPortalFromEdge(IEdgeModel edgeModel);
        IEdgePortalExitModel CreateExitPortalFromEdge(IEdgeModel edgeModel);
        IVariableNodeModel CreateVariableNode(IVariableDeclarationModel declarationModel, Vector2 position,
            GUID? guid = null, SpawnFlags spawnFlags = SpawnFlags.Default);
        IConstantNodeModel CreateConstantNode(TypeHandle constantTypeHandle, string constantName, Vector2 position,
            GUID? guid = null, Action<IConstantNodeModel> preDefine = null, SpawnFlags spawnFlags = SpawnFlags.Default);
        INodeModel CreateNode(Type nodeTypeToCreate, string nodeName, Vector2 position,
            GUID? guid = null, Action<INodeModel> preDefine = null, SpawnFlags spawnFlags = SpawnFlags.Default);
        INodeModel DuplicateNode(INodeModel sourceNode, Vector2 delta);
        IReadOnlyCollection<IGraphElementModel> DeleteNodes(IReadOnlyCollection<INodeModel> nodeModels, bool deleteConnections);
        // TODO JOCE: Would be worth attempting to extract this from VS.
        void CreateItemizedNode(State state, int nodeOffset, ref IPortModel outputPortModel);

        IEdgeModel CreateEdge(IPortModel toPort, IPortModel fromPort);
        IEdgeModel DuplicateEdge(IEdgeModel sourceEdge, INodeModel targetInputNode, INodeModel targetOutputNode);
        IReadOnlyCollection<IGraphElementModel> DeleteEdges(IReadOnlyCollection<IEdgeModel> edgeModels);

        void AddBadge(IBadgeModel badgeModel);
        IReadOnlyCollection<IGraphElementModel> DeleteBadges();
        IReadOnlyCollection<IGraphElementModel> DeleteBadgesOfType<T>() where T : IBadgeModel;

        IStickyNoteModel CreateStickyNote(Rect position, SpawnFlags spawnFlags = SpawnFlags.Default);
        IReadOnlyCollection<IGraphElementModel> DeleteStickyNotes(IReadOnlyCollection<IStickyNoteModel> stickyNotesModels);

        IPlacematModel CreatePlacemat(Rect position, SpawnFlags spawnFlags = SpawnFlags.Default);
        IReadOnlyCollection<IGraphElementModel> DeletePlacemats(IReadOnlyCollection<IPlacematModel> placematModels);

        List<IPortModel> GetCompatiblePorts(IPortModel startPortModel);

        bool CheckIntegrity(Verbosity errors);

        // PF FIXME: this looks like to be a hack support for PanToNode.
        IReadOnlyDictionary<GUID, INodeModel> NodesByGuid { get; }

        void UndoRedoPerformed();
        void CloneGraph(IGraphModel sourceGraphModel);
    }
}
