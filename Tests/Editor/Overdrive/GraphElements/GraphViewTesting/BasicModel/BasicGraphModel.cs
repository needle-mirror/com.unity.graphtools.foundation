using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements.Utilities
{
    public class BasicGraphModel : IGTFGraphModel
    {
        readonly List<BasicNodeModel> m_Nodes = new List<BasicNodeModel>();
        public IEnumerable<BasicNodeModel> Nodes => m_Nodes;

        readonly List<BasicEdgeModel> m_Edges = new List<BasicEdgeModel>();
        public IEnumerable<BasicEdgeModel> Edges => m_Edges;

        readonly List<BasicPlacematModel> m_Placemats = new List<BasicPlacematModel>();
        public IEnumerable<BasicPlacematModel> Placemats => m_Placemats;

        readonly List<BasicStickyNoteModel> m_StickyNoteModels = new List<BasicStickyNoteModel>();
        public IEnumerable<BasicStickyNoteModel> Stickies => m_StickyNoteModels;

        public TNodeModel CreateNode<TNodeModel>(string title = "") where TNodeModel : BasicNodeModel, new()
        {
            var node = new TNodeModel {Title = title, GraphModel = this};
            m_Nodes.Add(node);
            return node;
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

        public IReadOnlyList<IGTFNodeModel> NodeModels => m_Nodes;
        public IReadOnlyList<IGTFEdgeModel> EdgeModels => m_Edges;
        public IReadOnlyList<IGTFStickyNoteModel> StickyNoteModels => m_StickyNoteModels;
        public IReadOnlyList<IGTFPlacematModel> PlacematModels => m_Placemats;
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

        public IGTFEdgeModel CreateEdge(IGTFPortModel inputPort, IGTFPortModel outputPort)
        {
            var edge = new BasicEdgeModel(inputPort, outputPort);
            m_Edges.Add(edge);
            edge.GraphModel = this;
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

        public BasicStickyNoteModel CreateStickyNodeGTF(string title = "", string contents = "", Rect stickyRect = default)
        {
            var sticky = new BasicStickyNoteModel
            {
                Title = title,
                Contents = contents,
                PositionAndSize = stickyRect,
                Theme = StickyNoteTheme.Classic.ToString(),
                TextSize = StickyNoteFontSize.Small.ToString()
            };

            m_StickyNoteModels.Add(sticky);
            sticky.GraphModel = this;
            return sticky;
        }

        public void DeleteElements(IEnumerable<IGTFGraphElementModel> graphElementModels)
        {
            foreach (var model in graphElementModels)
            {
                switch (model)
                {
                    case BasicNodeModel basicNodeModel:
                        m_Nodes.Remove(basicNodeModel);
                        break;
                    case BasicEdgeModel basicEdgeModel:
                        m_Edges.Remove(basicEdgeModel);
                        break;
                    case BasicPlacematModel basicPlacematModel:
                        m_Placemats.Remove(basicPlacematModel);
                        break;
                    case BasicStickyNoteModel basicStickyNoteModel:
                        m_StickyNoteModels.Remove(basicStickyNoteModel);
                        break;
                }
            }
        }

        public List<IGTFPortModel> GetCompatiblePorts(IGTFPortModel startPort)
        {
            List<IGTFPortModel> compatiblePorts = this.GetPortModels().ToList().Where(p =>
                p.Direction != startPort.Direction &&
                p.PortDataType == startPort.PortDataType)
                .ToList();

            return startPort.NodeModel.AllowSelfConnect ? compatiblePorts : compatiblePorts.Where(p => p.NodeModel != startPort.NodeModel).ToList();
        }

        public IReadOnlyDictionary<GUID, IGTFNodeModel> NodesByGuid => NodeModels.ToDictionary(n => n.Guid);
        public GraphChangeList LastChanges => null;
        public IEnumerable<IGTFPortModel> GetConnections(IGTFPortModel portModel)
        {
            return GetEdgesConnections(portModel)
                .Select(e => portModel.Direction == Direction.Input ? e.FromPort : e.ToPort)
                .Where(p => p != null);
        }

        public IEnumerable<IGTFEdgeModel> GetEdgesConnections(IGTFPortModel portModel)
        {
            return EdgeModels.Where(e => portModel.Direction == Direction.Input ? BasicPortModel.Equivalent(e.ToPort, portModel) : BasicPortModel.Equivalent(e.FromPort, portModel));
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

        public void Disconnect(IGTFEdgeModel edge)
        {
            m_Edges.Remove(edge as BasicEdgeModel);
        }

        public BasicPlacematModel CreatePlacemat(string title, Rect posAndDim, int zOrder)
        {
            var placemat = new BasicPlacematModel(title);
            m_Placemats.Add(placemat);
            placemat.GraphModel = this;
            placemat.PositionAndSize = posAndDim;
            placemat.ZOrder = zOrder;
            return placemat;
        }

        public void Dispose() {}

        public void UndoRedoPerformed() {}
    }
}
