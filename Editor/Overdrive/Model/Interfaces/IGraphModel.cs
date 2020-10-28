using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

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

    public class NodeCreationParameters
    {
        public IVariableDeclarationModel VariableDeclarationModel;
        SpawnFlags SpawnFlags;
    }

    public enum DeleteConnections
    {
        False,
        True
    }

    public enum Verbosity
    {
        Errors,
        Verbose
    }

    public interface IGraphModel : IDisposable
    {
        Stencil Stencil { get; set; }
        IGraphAssetModel AssetModel { get; set; }

        void OnEnable();
        void OnDisable();

        string Name { get; set; }
        string FriendlyScriptName { get; }
        string GetSourceFilePath();

        IReadOnlyList<INodeModel> NodeModels { get; }
        IReadOnlyList<IEdgeModel> EdgeModels { get; }
        IReadOnlyList<IStickyNoteModel> StickyNoteModels { get; }
        IReadOnlyList<IPlacematModel> PlacematModels { get; }
        IList<IVariableDeclarationModel> VariableDeclarations { get; }
        IReadOnlyList<IDeclarationModel> PortalDeclarations { get; }
        string GetAssetPath();

        void MoveEdgeBefore(IEdgeModel toMove, IEdgeModel reference);
        void MoveEdgeAfter(IEdgeModel toMove, IEdgeModel reference);

        IVariableDeclarationModel CreateGraphVariableDeclaration(string variableName, TypeHandle variableDataType,
            ModifierFlags modifierFlags, bool isExposed, IConstant initializationModel = null, GUID? guid = null);
        void DeleteVariableDeclarations(IEnumerable<IVariableDeclarationModel> variableModels, bool deleteUsages);
        List<IVariableDeclarationModel> DuplicateGraphVariableDeclarations(List<IVariableDeclarationModel> variableDeclarationModels);
        void ReorderGraphVariableDeclaration(IVariableDeclarationModel variableDeclarationModel, int index);

        IVariableDeclarationModel CreateGraphPortalDeclaration(string portalName, SpawnFlags spawnFlags = SpawnFlags.Default);
        IEdgePortalModel CreateOppositePortal(IEdgePortalModel edgePortalModel, Vector2 position, SpawnFlags spawnFlags);
        IEdgePortalEntryModel CreateEntryPortalFromEdge(IEdgeModel edgeModel);
        IEdgePortalExitModel CreateExitPortalFromEdge(IEdgeModel edgeModel);

        IVariableNodeModel CreateVariableNode(IVariableDeclarationModel declarationModel, Vector2 position, SpawnFlags spawnFlags = SpawnFlags.Default, GUID? guid = null);

        IConstantNodeModel CreateConstantNode(string constantName,
            TypeHandle constantTypeHandle, Vector2 position, SpawnFlags spawnFlags = SpawnFlags.Default, GUID? guid = null, Action<IConstantNodeModel> preDefine = null);

        TNodeType CreateNode<TNodeType>(string nodeName = "", Vector2 position = default, SpawnFlags spawnFlags = SpawnFlags.Default, Action<TNodeType> preDefine = null, GUID? guid = null) where TNodeType : class, INodeModel;
        INodeModel CreateNode(Type nodeTypeToCreate, string nodeName, Vector2 position, SpawnFlags spawnFlags = SpawnFlags.Default, Action<INodeModel> preDefine = null, GUID? guid = null);
        void DeleteNode(INodeModel nodeModel, DeleteConnections deleteConnections);
        void DeleteNodes(IReadOnlyCollection<INodeModel> nodesToDelete, DeleteConnections deleteConnections);
        INodeModel DuplicateNode(INodeModel copiedNode, Dictionary<INodeModel, INodeModel> mapping, Vector2 delta);

        void CreateItemizedNode(State state, int nodeOffset, ref IPortModel outputPortModel);

        IEdgeModel CreateEdge(IPortModel inputPort, IPortModel outputPort);
        void DeleteEdge(IEdgeModel edgeModel);
        void DeleteEdges(IEnumerable<IEdgeModel> edgeModels);

        IStickyNoteModel CreateStickyNote(Rect position, SpawnFlags dataSpawnFlags = SpawnFlags.Default);
        void DeleteStickyNotes(IStickyNoteModel[] stickyNotesToDelete);

        IPlacematModel CreatePlacemat(string title, Rect position, SpawnFlags dataSpawnFlags = SpawnFlags.Default);
        void DeletePlacemats(IPlacematModel[] placematsToDelete);

        void DeleteElements(IEnumerable<IGraphElementModel> graphElementModels);

        List<IPortModel> GetCompatiblePorts(IPortModel startPortModel);

        // PF FIXME: this looks like to be a hack support for PanToNode.
        IReadOnlyDictionary<GUID, INodeModel> NodesByGuid { get; }

        // PF FIXME: Do we really need this? Change tracking should be handled by Redux.
        GraphChangeList LastChanges { get; }
        IEnumerable<IPortModel> GetConnections(IPortModel portModel);
        IEnumerable<IEdgeModel> GetEdgesConnections(IPortModel portModel);
        IEnumerable<IEdgeModel> GetEdgesConnections(INodeModel nodeModel);

        // Developer tools
        void QuickCleanup();
        bool CheckIntegrity(Verbosity errors);
        CompilationResult Compile(ITranslator translator);
        void ResetChangeList();
        void Repair();
        void UndoRedoPerformed();
    }
}
