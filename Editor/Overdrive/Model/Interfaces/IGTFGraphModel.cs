using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Model
{
    /// <summary>
    /// Spawn flags dictates multiple operations during the NodeModels creation.
    /// </summary>
    [Flags]
    public enum SpawnFlags
    {
        None   = 0,
        /// <summary>
        /// During the NodeModel creation, it registers an undo point so the spawning/adding can be undoable/redoable
        /// </summary>
        Undoable  = 1 << 0,
        /// <summary>
        /// During the NodeModel creation, it with a SerializableAsset under it to make it serializable in the asset.
        /// </summary>
        CreateNodeAsset = 1 << 1,
        /// <summary>
        /// The created NodeModel is not added to a Stack/Graph. Useful for display only purposes.
        /// </summary>
        Orphan  = 1 << 2,
        /// <summary>
        /// This include the SpawnFlags.Orphan and SpawnFlags.CreateNodeAsset
        /// </summary>
        Default = Undoable | CreateNodeAsset,
    }

    public static class SpawnFlagsExtensions
    {
        public static bool IsOrphan(this SpawnFlags f) => (f & SpawnFlags.Orphan) != 0;
        public static bool IsUndoable(this SpawnFlags f) => (f & SpawnFlags.Undoable) != 0;
        public static bool IsSerializable(this SpawnFlags f) => (f & SpawnFlags.CreateNodeAsset) != 0;
    }

    public class NodeCreationParameters
    {
        public IGTFVariableDeclarationModel VariableDeclarationModel;
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

    public interface IGTFGraphModel : IDisposable
    {
        Stencil Stencil { get; }
        IGTFGraphAssetModel AssetModel { get; set; }

        string Name { get; set; }
        string FriendlyScriptName { get; }
        string GetSourceFilePath();

        IReadOnlyList<IGTFNodeModel> NodeModels { get; }
        IReadOnlyList<IGTFEdgeModel> EdgeModels { get; }
        IReadOnlyList<IGTFStickyNoteModel> StickyNoteModels { get; }
        IReadOnlyList<IGTFPlacematModel> PlacematModels { get; }
        IList<IGTFVariableDeclarationModel> VariableDeclarations { get; }
        IReadOnlyList<IDeclarationModel> PortalDeclarations { get; }
        string GetAssetPath();

        void MoveEdgeBefore(IGTFEdgeModel toMove, IGTFEdgeModel reference);
        void MoveEdgeAfter(IGTFEdgeModel toMove, IGTFEdgeModel reference);

        IGTFVariableDeclarationModel CreateGraphVariableDeclaration(string variableName, TypeHandle variableDataType,
            ModifierFlags modifierFlags, bool isExposed, IConstant initializationModel = null, GUID? guid = null);
        void DeleteVariableDeclarations(IEnumerable<IGTFVariableDeclarationModel> variableModels, bool deleteUsages);
        List<IGTFVariableDeclarationModel> DuplicateGraphVariableDeclarations(List<IGTFVariableDeclarationModel> variableDeclarationModels);
        void ReorderGraphVariableDeclaration(IGTFVariableDeclarationModel variableDeclarationModel, int index);

        IGTFVariableDeclarationModel CreateGraphPortalDeclaration(string portalName, SpawnFlags spawnFlags = SpawnFlags.Default);
        IGTFEdgePortalModel CreateOppositePortal(IGTFEdgePortalModel edgePortalModel, Vector2 position, SpawnFlags spawnFlags);

        IGTFVariableNodeModel CreateVariableNode(IGTFVariableDeclarationModel declarationModel, Vector2 position, SpawnFlags spawnFlags = SpawnFlags.Default, GUID? guid = null);

        IGTFConstantNodeModel CreateConstantNode(string constantName,
            TypeHandle constantTypeHandle, Vector2 position, SpawnFlags spawnFlags = SpawnFlags.Default, GUID? guid = null, Action<IGTFConstantNodeModel> preDefine = null);

        IConstant CreateConstantValue(TypeHandle constantTypeHandle, Action<IConstant> preDefine = null);

        TNodeType CreateNode<TNodeType>(string nodeName = "", Vector2 position = default, SpawnFlags spawnFlags = SpawnFlags.Default, Action<TNodeType> preDefine = null, GUID? guid = null) where TNodeType : class, IGTFNodeModel;
        IGTFNodeModel CreateNode(Type nodeTypeToCreate, string nodeName, Vector2 position, SpawnFlags spawnFlags = SpawnFlags.Default, Action<IGTFNodeModel> preDefine = null, GUID? guid = null);
        void DeleteNode(IGTFNodeModel nodeModel, DeleteConnections deleteConnections);
        void DeleteNodes(IReadOnlyCollection<IGTFNodeModel> nodesToDelete, DeleteConnections deleteConnections);
        IGTFNodeModel DuplicateNode(IGTFNodeModel copiedNode, Dictionary<IGTFNodeModel, IGTFNodeModel> mapping, Vector2 delta);

        IGTFEdgeModel CreateEdge(IGTFPortModel inputPort, IGTFPortModel outputPort);
        void DeleteEdge(IGTFEdgeModel edgeModel);
        void DeleteEdges(IEnumerable<IGTFEdgeModel> edgeModels);

        IGTFStickyNoteModel CreateStickyNote(Rect position, SpawnFlags dataSpawnFlags = SpawnFlags.Default);
        void DeleteStickyNotes(IGTFStickyNoteModel[] stickyNotesToDelete);

        IGTFPlacematModel CreatePlacemat(string title, Rect position, SpawnFlags dataSpawnFlags = SpawnFlags.Default);
        void DeletePlacemats(IGTFPlacematModel[] placematsToDelete);

        void DeleteElements(IEnumerable<IGTFGraphElementModel> graphElementModels);

        List<IGTFPortModel> GetCompatiblePorts(IGTFPortModel startPortModel);

        // PF FIXME: this looks like to be a hack support for PanToNode.
        IReadOnlyDictionary<GUID, IGTFNodeModel> NodesByGuid { get; }

        // PF FIXME: Do we really need this? Change tracking should be handled by Redux.
        GraphChangeList LastChanges { get; }
        IEnumerable<IGTFPortModel> GetConnections(IGTFPortModel portModel);
        IEnumerable<IGTFEdgeModel> GetEdgesConnections(IGTFPortModel portModel);

        // Developer tools
        void QuickCleanup();
        bool CheckIntegrity(Verbosity errors);
        CompilationResult Compile(ITranslator translator);
        void ResetChangeList();
        void Repair();
        void UndoRedoPerformed();
    }
}
