using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIFromModelTests
{
    class GraphModel : IGTFGraphModel
    {
        List<IGTFEdgeModel> m_EdgeModels = new List<IGTFEdgeModel>();

        public void Dispose()
        {
        }

        public Stencil Stencil => null;
        IGTFGraphAssetModel IGTFGraphModel.AssetModel { get; set; }
        public string Name { get; set; }
        public IGTFGraphAssetModel AssetModel => null;
        public string FriendlyScriptName => "";
        public string TypeName { get; }
        public string GetSourceFilePath()
        {
            throw new NotImplementedException();
        }

        public List<IGTFNodeModel> m_Nodes = new List<IGTFNodeModel>();
        public IReadOnlyList<IGTFNodeModel> NodeModels => m_Nodes;
        public IReadOnlyList<IGTFEdgeModel> EdgeModels => m_EdgeModels;
        public IReadOnlyList<IGTFStickyNoteModel> StickyNoteModels => new IGTFStickyNoteModel[0];
        public IReadOnlyList<IGTFPlacematModel> PlacematModels => new IGTFPlacematModel[0];
        public IList<IGTFVariableDeclarationModel> VariableDeclarations { get; }
        public IReadOnlyList<IDeclarationModel> PortalDeclarations { get; }

        public string GetAssetPath()
        {
            throw new NotImplementedException();
        }

        public void MoveEdgeBefore(IGTFEdgeModel toMove, IGTFEdgeModel reference)
        {
            throw new NotImplementedException();
        }

        public void MoveEdgeAfter(IGTFEdgeModel toMove, IGTFEdgeModel reference)
        {
            throw new NotImplementedException();
        }

        public IGTFVariableDeclarationModel CreateGraphVariableDeclaration(string variableName,
            TypeHandle variableDataType, ModifierFlags modifierFlags, bool isExposed,
            IConstant initializationModel = null, GUID? guid = null)
        {
            throw new NotImplementedException();
        }

        public void DeleteVariableDeclarations(IEnumerable<IGTFVariableDeclarationModel> variableModels, bool deleteUsages)
        {
            throw new NotImplementedException();
        }

        public List<IGTFVariableDeclarationModel> DuplicateGraphVariableDeclarations(List<IGTFVariableDeclarationModel> variableDeclarationModels)
        {
            throw new NotImplementedException();
        }

        public void ReorderGraphVariableDeclaration(IGTFVariableDeclarationModel variableDeclarationModel, int index)
        {
            throw new NotImplementedException();
        }

        public IGTFVariableDeclarationModel CreateGraphPortalDeclaration(string portalName, SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            throw new NotImplementedException();
        }

        public IGTFEdgePortalModel CreateOppositePortal(IGTFEdgePortalModel edgePortalModel, Vector2 position, SpawnFlags spawnFlags)
        {
            throw new NotImplementedException();
        }

        public IGTFVariableNodeModel CreateVariableNode(IGTFVariableDeclarationModel declarationModel, Vector2 position, SpawnFlags spawnFlags = SpawnFlags.Default, GUID? guid = null)
        {
            throw new NotImplementedException();
        }

        public IGTFConstantNodeModel CreateConstantNode(string constantName, TypeHandle constantTypeHandle, Vector2 position, SpawnFlags spawnFlags = SpawnFlags.Default, GUID? guid = null, Action<IGTFConstantNodeModel> preDefine = null)
        {
            throw new NotImplementedException();
        }

        public IConstant CreateConstantValue(TypeHandle constantTypeHandle, Action<IConstant> preDefine = null)
        {
            throw new NotImplementedException();
        }

        public TNodeType CreateNode<TNodeType>(string nodeName, Vector2 position, SpawnFlags spawnFlags, Action<TNodeType> preDefine = null, GUID? guid = null) where TNodeType : class, IGTFNodeModel
        {
            throw new NotImplementedException();
        }

        public IGTFNodeModel CreateNode(Type nodeTypeToCreate, string nodeName, Vector2 position, SpawnFlags spawnFlags, Action<IGTFNodeModel> preDefine = null, GUID? guid = null)
        {
            throw new NotImplementedException();
        }

        public void DeleteNode(IGTFNodeModel nodeModel, DeleteConnections deleteConnections)
        {
            throw new NotImplementedException();
        }

        public void DeleteNodes(IReadOnlyCollection<IGTFNodeModel> nodesToDelete, DeleteConnections deleteConnections)
        {
            throw new NotImplementedException();
        }

        public IGTFNodeModel DuplicateNode(IGTFNodeModel copiedNode, Dictionary<IGTFNodeModel, IGTFNodeModel> mapping, Vector2 delta)
        {
            throw new NotImplementedException();
        }

        public void AddNode(IGTFNodeModel node)
        {
            m_Nodes.Add(node);
        }

        public IGTFEdgeModel CreateEdge(IGTFPortModel inputPort, IGTFPortModel outputPort)
        {
            var edge = new EdgeModel(inputPort, outputPort);
            edge.GraphModel = this;
            m_EdgeModels.Add(edge);
            return edge;
        }

        public void DeleteEdge(IGTFEdgeModel edgeModel)
        {
            throw new NotImplementedException();
        }

        public void DeleteEdges(IEnumerable<IGTFEdgeModel> edgeModels)
        {
            throw new NotImplementedException();
        }

        public IGTFStickyNoteModel CreateStickyNote(Rect position, SpawnFlags dataSpawnFlags = SpawnFlags.Default)
        {
            throw new NotImplementedException();
        }

        public void DeleteStickyNotes(IGTFStickyNoteModel[] stickyNotesToDelete)
        {
            throw new NotImplementedException();
        }

        public IGTFPlacematModel CreatePlacemat(string title, Rect position, SpawnFlags dataSpawnFlags = SpawnFlags.Default)
        {
            throw new NotImplementedException();
        }

        public void DeletePlacemats(IGTFPlacematModel[] placematsToDelete)
        {
            throw new NotImplementedException();
        }

        public void DeleteElements(IEnumerable<IGTFGraphElementModel> graphElementModels)
        {
        }

        public List<IGTFPortModel> GetCompatiblePorts(IGTFPortModel startPort)
        {
            return this.GetPortModels().ToList().Where(p =>
                p.Direction != startPort.Direction &&
                p.NodeModel != startPort.NodeModel &&
                p.PortDataType == startPort.PortDataType)
                .ToList();
        }

        public IReadOnlyDictionary<GUID, IGTFNodeModel> NodesByGuid => NodeModels.ToDictionary(e => e.Guid);
        public GraphChangeList LastChanges { get; }
        public IEnumerable<IGTFPortModel> GetConnections(IGTFPortModel portModel)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IGTFEdgeModel> GetEdgesConnections(IGTFPortModel portModel)
        {
            throw new NotImplementedException();
        }

        public void QuickCleanup()
        {
            throw new NotImplementedException();
        }

        public bool CheckIntegrity(Verbosity errors)
        {
            throw new NotImplementedException();
        }

        public CompilationResult Compile(ITranslator translator)
        {
            throw new NotImplementedException();
        }

        public void ResetChangeList()
        {
        }

        public void Repair()
        {
            throw new NotImplementedException();
        }

        public void UndoRedoPerformed() {}
    }
}
