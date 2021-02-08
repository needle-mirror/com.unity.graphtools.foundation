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

    /// <summary>
    /// Interface for a model that represents a graph.
    /// </summary>
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
            string variableName, ModifierFlags modifierFlags, bool isExposed, IConstant initializationModel = null, SerializableGUID guid = default);
        IReadOnlyCollection<IGraphElementModel> DeleteVariableDeclarations(IReadOnlyCollection<IVariableDeclarationModel> variableModels, bool deleteUsages);
        IVariableDeclarationModel DuplicateGraphVariableDeclaration(IVariableDeclarationModel sourceModel);

        IDeclarationModel CreateGraphPortalDeclaration(string portalName, SpawnFlags spawnFlags = SpawnFlags.Default);
        IEdgePortalModel CreateOppositePortal(IEdgePortalModel edgePortalModel, Vector2 position, SpawnFlags spawnFlags = SpawnFlags.Default);
        IEdgePortalEntryModel CreateEntryPortalFromEdge(IEdgeModel edgeModel);
        IEdgePortalExitModel CreateExitPortalFromEdge(IEdgeModel edgeModel);

        /// <summary>
        /// Creates a new variable node in the graph.
        /// </summary>
        /// <param name="declarationModel">The declaration for the variable.</param>
        /// <param name="position">The position of the node to create.</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item.</param>
        /// <param name="spawnFlags">The flags specifying how the node is to be spawned.</param>
        /// <returns>The newly created variable node.</returns>
        IVariableNodeModel CreateVariableNode(IVariableDeclarationModel declarationModel, Vector2 position,
            SerializableGUID guid = default, SpawnFlags spawnFlags = SpawnFlags.Default);

        /// <summary>
        /// Creates a new constant node in the graph.
        /// </summary>
        /// <param name="constantTypeHandle">The type of the new constant node to create.</param>
        /// <param name="constantName">The name of the constant node to create.</param>
        /// <param name="position">The position of the node to create.</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item.</param>
        /// <param name="preDefine">A method to be called before the constant node is created.</param>
        /// <param name="spawnFlags">The flags specifying how the node is to be spawned.</param>
        /// <returns>The newly created constant node.</returns>
        IConstantNodeModel CreateConstantNode(TypeHandle constantTypeHandle, string constantName, Vector2 position,
            SerializableGUID guid = default, Action<IConstantNodeModel> preDefine = null, SpawnFlags spawnFlags = SpawnFlags.Default);

        /// <summary>
        /// Creates a new node in the graph.
        /// </summary>
        /// <param name="nodeTypeToCreate">The type of the new node to create.</param>
        /// <param name="nodeName">The name of the node to create.</param>
        /// <param name="position">The position of the node to create.</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item.</param>
        /// <param name="preDefine">A method to be called before the node is created.</param>
        /// <param name="spawnFlags">The flags specifying how the node is to be spawned.</param>
        /// <returns>The newly created node.</returns>
        INodeModel CreateNode(Type nodeTypeToCreate, string nodeName, Vector2 position,
            SerializableGUID guid = default, Action<INodeModel> preDefine = null, SpawnFlags spawnFlags = SpawnFlags.Default);
        INodeModel DuplicateNode(INodeModel sourceNode, Vector2 delta);
        IReadOnlyCollection<IGraphElementModel> DeleteNodes(IReadOnlyCollection<INodeModel> nodeModels, bool deleteConnections);
        // TODO JOCE: Would be worth attempting to extract this from VS.
        void CreateItemizedNode(GraphToolState graphToolState, int nodeOffset, ref IPortModel outputPortModel);

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

        /// <summary>
        /// A dictionary that associates node models present in the graph to their GUIDs.
        /// </summary>
        // PF FIXME: this looks like to be a hack support for PanToNode.
        IReadOnlyDictionary<SerializableGUID, INodeModel> NodesByGuid { get; }

        void UndoRedoPerformed();
        void CloneGraph(IGraphModel sourceGraphModel);
    }
}
