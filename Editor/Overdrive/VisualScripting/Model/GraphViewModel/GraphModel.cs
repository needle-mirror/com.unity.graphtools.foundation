using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Scripting.APIUpdating;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel
{
    [Serializable]
    [MovedFrom(false, "UnityEditor.VisualScripting.GraphViewModel", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public abstract class GraphModel : IGraphModel, IGTFGraphModel
    {
        [SerializeField]
        ModelState m_State;
        [SerializeField]
        GraphAssetModel m_AssetModel;

        [SerializeReference]
        protected List<INodeModel> m_GraphNodeModels;
        [SerializeField]
        protected List<EdgeModel> m_EdgeModels;

        [SerializeField]
        protected List<StickyNoteModel> m_StickyNoteModels;

        [SerializeField]
        protected List<PlacematModel> m_PlacematModels;

        [SerializeReference]
        Stencil m_Stencil;

        const float k_IOHorizontalOffset = 150;
        const float k_IOVerticalOffset = 40;

        public abstract IList<VariableDeclarationModel> VariableDeclarations { get; }

        public abstract IList<VariableDeclarationModel> PortalDeclarations { get; }

        public GraphChangeList LastChanges
        {
            get => m_LastChanges ?? (m_LastChanges = new GraphChangeList());
            private set => m_LastChanges = value;
        }

        IGraphChangeList IGraphModel.LastChanges => LastChanges;

        protected GraphModel()
        {
            LastChanges = new GraphChangeList();
            if (m_GraphNodeModels == null)
                m_GraphNodeModels = new List<INodeModel>();
            if (m_EdgeModels == null)
                m_EdgeModels = new List<EdgeModel>();
            if (m_NodesByGuid == null)
                m_NodesByGuid = new Dictionary<GUID, INodeModel>();
            if (m_StickyNoteModels == null)
                m_StickyNoteModels = new List<StickyNoteModel>();
            if (m_PlacematModels == null)
                m_PlacematModels = new List<PlacematModel>();
        }

        public virtual string Name => name;

        public ModelState State
        {
            get => m_State;
            set => m_State = value;
        }

        public IGraphAssetModel AssetModel
        {
            get => m_AssetModel;
            set => m_AssetModel = (GraphAssetModel)value;
        }

        Dictionary<GUID, INodeModel> m_NodesByGuid;
        public string name;
        GraphChangeList m_LastChanges;

        public IReadOnlyDictionary<GUID, INodeModel> NodesByGuid => m_NodesByGuid ?? (m_NodesByGuid = new Dictionary<GUID, INodeModel>());

        public IReadOnlyList<INodeModel> NodeModels => m_GraphNodeModels;
        public IReadOnlyList<IEdgeModel> EdgeModels => m_EdgeModels;
        public IReadOnlyList<IStickyNoteModel> StickyNoteModels => m_StickyNoteModels;
        public IReadOnlyList<IPlacematModel> PlacematModels => m_PlacematModels;

        public Stencil Stencil
        {
            get => m_Stencil;
            set => m_Stencil = value;
        }

        public enum DeleteConnections
        {
            False,
            True
        }

        public string GetAssetPath()
        {
            return AssetDatabase.GetAssetPath(m_AssetModel);
        }

        public virtual string GetUniqueName(string baseName)
        {
            return baseName;
        }

        public TNodeType CreateNode<TNodeType>(string nodeName = "", Vector2 position = default, SpawnFlags spawnFlags = SpawnFlags.Default, Action<TNodeType> preDefineSetup = null, GUID? guid = null) where TNodeType : NodeModel
        {
            return (TNodeType)CreateNode(typeof(TNodeType), nodeName, position, spawnFlags, preDefineSetup == null ? (Action<NodeModel>)null : n => preDefineSetup.Invoke((TNodeType)n), guid);
        }

        public INodeModel CreateNode(Type nodeTypeToCreate, string nodeName, Vector2 position, SpawnFlags spawnFlags = SpawnFlags.Default, Action<NodeModel> preDefineSetup = null, GUID? guid = null)
        {
            return CreateNodeInternal(nodeTypeToCreate, nodeName, position, spawnFlags, preDefineSetup, guid);
        }

        public void MoveEdgeBefore(IEdgeModel toMove, IEdgeModel reference)
        {
            m_EdgeModels.Remove((EdgeModel)toMove);
            m_EdgeModels.Insert(m_EdgeModels.IndexOf((EdgeModel)reference), (EdgeModel)toMove);
        }

        public void MoveEdgeAfter(IEdgeModel toMove, IEdgeModel reference)
        {
            m_EdgeModels.Remove((EdgeModel)toMove);
            m_EdgeModels.Insert(m_EdgeModels.IndexOf((EdgeModel)reference) + 1, (EdgeModel)toMove);
        }

        internal INodeModel CreateNodeInternal(Type nodeTypeToCreate, string nodeName, Vector2 position,
            SpawnFlags spawnFlags = SpawnFlags.Default, Action<NodeModel> preDefineSetup = null, GUID? guid = null)
        {
            if (nodeTypeToCreate == null)
                throw new InvalidOperationException("Cannot create node with a null type");
            var nodeModel = (NodeModel)Activator.CreateInstance(nodeTypeToCreate);

            nodeModel.Title = nodeName ?? nodeTypeToCreate.Name;
            nodeModel.Position = position;
            nodeModel.Guid = guid ?? GUID.Generate();
            nodeModel.AssetModel = AssetModel;
            preDefineSetup?.Invoke(nodeModel);
            nodeModel.DefineNode();
            if (!spawnFlags.IsOrphan())
            {
                if (spawnFlags.IsUndoable())
                    AddNode(nodeModel);
                else
                    AddNodeNoUndo(nodeModel);
                EditorUtility.SetDirty(m_AssetModel);
            }
            return nodeModel;
        }

        public void AddNode(INodeModel nodeModel)
        {
            Undo.RegisterCompleteObjectUndo(m_AssetModel, "Add Node");
            AddNodeInternal(nodeModel);
            LastChanges?.ChangedElements.Add(nodeModel);
        }

        public void AddNodeNoUndo(INodeModel nodeModel)
        {
            Utility.SaveAssetIntoObject(nodeModel, (Object)AssetModel);
            AddNodeInternal(nodeModel);
            LastChanges?.ChangedElements.Add(nodeModel);
        }

        void AddNodeInternal(INodeModel nodeModel)
        {
            ((NodeModel)nodeModel).AssetModel = AssetModel;
            m_GraphNodeModels.Add(nodeModel);
            if (m_NodesByGuid == null)
                m_NodesByGuid = new Dictionary<GUID, INodeModel>();
            m_NodesByGuid.Add(nodeModel.Guid, nodeModel);
        }

        public void DeleteNodes(IReadOnlyCollection<INodeModel> nodesToDelete, DeleteConnections deleteConnections)
        {
            foreach (var node in nodesToDelete)
                DeleteNode(node, deleteConnections);
        }

        public void DeleteNode(INodeModel nodeModel, DeleteConnections deleteConnections)
        {
            Undo.RegisterCompleteObjectUndo(m_AssetModel, "Delete Node");

            var model = (NodeModel)nodeModel;

            if (LastChanges != null)
                LastChanges.DeletedElements += 1;
            m_GraphNodeModels.Remove(model);

            if (deleteConnections == DeleteConnections.True)
            {
                var connectedEdges = nodeModel.GetConnectedEdges().ToList();
                DeleteEdges(connectedEdges);
            }
            UnregisterNodeGuid(model.Guid);

            model.Destroy();
        }

        internal void RegisterNodeGuid(INodeModel model)
        {
            m_NodesByGuid.Add(model.Guid, model);
        }

        internal void UnregisterNodeGuid(GUID nodeModelGuid)
        {
            m_NodesByGuid.Remove(nodeModelGuid);
        }

        internal void MoveNode(INodeModel nodeToMove, Vector2 newPosition)
        {
            var nodeModel = (NodeModel)nodeToMove;
            Undo.RegisterCompleteObjectUndo(m_AssetModel, "Move");
            nodeModel.Move(newPosition);
        }

        public IGTFEdgeModel CreateEdgeGTF(IGTFPortModel inputPort, IGTFPortModel outputPort)
        {
            return CreateEdge(inputPort as IPortModel, outputPort as IPortModel) as IGTFEdgeModel;
        }

        public IEdgeModel CreateEdge(IPortModel inputPort, IPortModel outputPort)
        {
            var existing = EdgesConnectedToPorts(inputPort, outputPort);
            if (existing != null)
                return existing;

            if (!Stencil.ValidateEdgeConnection(inputPort, outputPort))
                return null;

            var edgeModel = CreateOrphanEdge(inputPort, outputPort);
            AddEdge(edgeModel, inputPort, outputPort);

            return edgeModel;
        }

        public IEdgeModel CreateOrphanEdge(IPortModel input, IPortModel output)
        {
            Assert.IsNotNull(input);
            Assert.IsNotNull(input.NodeModel);
            Assert.IsNotNull(output);
            Assert.IsNotNull(output.NodeModel);

            var edgeModel = new EdgeModel(this, input, output) { EdgeLabel = String.Empty };

            input.NodeModel.OnConnection(input, output);
            output.NodeModel.OnConnection(output, input);

            return edgeModel;
        }

        void AddEdge(IEdgeModel edgeModel, IPortModel inputPort, IPortModel outputPort)
        {
            Undo.RegisterCompleteObjectUndo(m_AssetModel, "Add Edge");
            ((EdgeModel)edgeModel).VSGraphModel = this;
            m_EdgeModels.Add((EdgeModel)edgeModel);
            LastChanges?.ChangedElements.Add(edgeModel);
            LastChanges?.ChangedElements.Add(inputPort.NodeModel);
            LastChanges?.ChangedElements.Add(outputPort.NodeModel);
        }

        public void DeleteEdge(IPortModel input, IPortModel output)
        {
            DeleteEdges(m_EdgeModels.Where(x => x.ToPort == input && x.FromPort == output));
        }

        public void DeleteEdge(IEdgeModel edgeModel)
        {
            Undo.RegisterCompleteObjectUndo(m_AssetModel, "Delete Edge");
            var model = (EdgeModel)edgeModel;

            edgeModel.InputPortModel?.NodeModel?.OnDisconnection(edgeModel.InputPortModel, edgeModel.OutputPortModel);
            edgeModel.OutputPortModel?.NodeModel?.OnDisconnection(edgeModel.OutputPortModel, edgeModel.InputPortModel);

            LastChanges?.ChangedElements.Add(edgeModel.InputPortModel?.NodeModel);
            LastChanges?.ChangedElements.Add(edgeModel.OutputPortModel?.NodeModel);

            m_EdgeModels.Remove(model);
            if (LastChanges != null)
            {
                LastChanges.DeleteEdgeModels.Add(model);
                LastChanges.DeletedElements += 1;
            }
        }

        public void DeleteEdges(IEnumerable<IEdgeModel> edgeModels)
        {
            var edgesCopy = edgeModels.ToList();
            foreach (var edgeModel in edgesCopy)
                DeleteEdge(edgeModel);
        }

        public void DeleteElements(IEnumerable<IGTFGraphElementModel> graphElementModels)
        {
            foreach (var model in graphElementModels)
            {
                switch (model)
                {
                    case INodeModel nodeModel:
                        m_GraphNodeModels.Remove(nodeModel);
                        break;
                    case EdgeModel edgeModel:
                        m_EdgeModels.Remove(edgeModel);
                        break;
                    case StickyNoteModel stickyNoteModel:
                        m_StickyNoteModels.Remove(stickyNoteModel);
                        break;
                    case PlacematModel placematModel:
                        m_PlacematModels.Remove(placematModel);
                        break;
                }
            }
        }

        public IStickyNoteModel CreateStickyNote(Rect position, SpawnFlags dataSpawnFlags = SpawnFlags.Default)
        {
            var stickyNodeModel = (StickyNoteModel)CreateOrphanStickyNote(position);
            if (!dataSpawnFlags.IsOrphan())
                AddStickyNote(stickyNodeModel);

            return stickyNodeModel;
        }

        IStickyNoteModel CreateOrphanStickyNote(Rect position)
        {
            var stickyNodeModel = new StickyNoteModel();
            stickyNodeModel.PositionAndSize = position;
            stickyNodeModel.VSGraphModel = this;

            return stickyNodeModel;
        }

        void AddStickyNote(IStickyNoteModel model)
        {
            var stickyNodeModel = (StickyNoteModel)model;

            Undo.RegisterCompleteObjectUndo(m_AssetModel, "Add Sticky Note");
            LastChanges?.ChangedElements.Add(stickyNodeModel);
            stickyNodeModel.VSGraphModel = this;
            m_StickyNoteModels.Add(stickyNodeModel);
        }

        void DeleteStickyNote(IStickyNoteModel stickyNoteModel)
        {
            Undo.RegisterCompleteObjectUndo(m_AssetModel, "Delete Sticky Note");
            var model = (StickyNoteModel)stickyNoteModel;

            m_StickyNoteModels.Remove(model);
            if (LastChanges != null)
                LastChanges.DeletedElements += 1;

            model.Destroy();
        }

        const string k_DefaultPlacematName = "Placemat";
        public IPlacematModel CreatePlacemat(string title, Rect position, SpawnFlags dataSpawnFlags = SpawnFlags.Default)
        {
            var placematModel = (PlacematModel)CreateOrphanPlacemat(title ?? k_DefaultPlacematName, position);
            if (!dataSpawnFlags.IsOrphan())
                AddPlacemat(placematModel);

            return placematModel;
        }

        IPlacematModel CreateOrphanPlacemat(string title, Rect position)
        {
            var placematModel = new PlacematModel();
            placematModel.Title = title;
            placematModel.PositionAndSize = position;
            placematModel.VSGraphModel = this;
            placematModel.ZOrder = GetPlacematTopZOrder();
            return placematModel;
        }

        int GetPlacematTopZOrder()
        {
            int maxZ = Int32.MinValue;
            foreach (var model in m_PlacematModels)
            {
                maxZ = Math.Max(model.ZOrder, maxZ);
            }
            return maxZ == Int32.MinValue ? 1 : maxZ + 1;
        }

        void AddPlacemat(IPlacematModel model)
        {
            var placematModel = (PlacematModel)model;

            Undo.RegisterCompleteObjectUndo(m_AssetModel, "Add Placemat");
            LastChanges?.ChangedElements.Add(placematModel);
            placematModel.VSGraphModel = this;
            m_PlacematModels.Add(placematModel);
        }

        void DeletePlacemat(IPlacematModel placematModel)
        {
            Undo.RegisterCompleteObjectUndo(m_AssetModel, "Delete Placemat");
            var model = (PlacematModel)placematModel;

            m_PlacematModels.Remove(model);
            if (LastChanges != null)
            {
                if (placematModel.HiddenElementsGuid != null)
                {
                    LastChanges.ChangedElements.AddRange(m_GraphNodeModels.Where(e => placematModel.HiddenElementsGuid.Contains(e.GetId().ToString())));
                }
                LastChanges.DeletedElements += 1;
            }

            model.Destroy();
        }

        static readonly Vector2 k_PortalOffset = Vector2.right * 150;

        public IEdgePortalModel CreateOppositePortal(IEdgePortalModel edgePortalModel, SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            var offset = Vector2.zero;
            switch (edgePortalModel)
            {
                case IEdgePortalEntryModel _:
                    offset = k_PortalOffset;
                    break;
                case IEdgePortalExitModel _:
                    offset = -k_PortalOffset;
                    break;
            }
            var currentPos = ((EdgePortalModel)edgePortalModel).Position;
            return CreateOppositePortal(edgePortalModel, currentPos + offset, spawnFlags);
        }

        public IEdgePortalModel CreateOppositePortal(IEdgePortalModel edgePortalModel, Vector2 position, SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            EdgePortalModel newPortal = null;
            Type oppositeType = null;
            switch (edgePortalModel)
            {
                case ExecutionEdgePortalEntryModel _:
                    oppositeType = typeof(ExecutionEdgePortalExitModel);
                    break;
                case ExecutionEdgePortalExitModel _:
                    oppositeType = typeof(ExecutionEdgePortalEntryModel);
                    break;
                case DataEdgePortalEntryModel _:
                    oppositeType = typeof(DataEdgePortalExitModel);
                    break;
                case DataEdgePortalExitModel _:
                    oppositeType = typeof(DataEdgePortalEntryModel);
                    break;
            }

            if (oppositeType != null)
                newPortal = (EdgePortalModel)CreateNode(oppositeType, edgePortalModel.Title, position, spawnFlags);

            if (newPortal != null)
            {
                newPortal.DeclarationModel = edgePortalModel.DeclarationModel;
            }

            return newPortal;
        }

        protected internal virtual void OnEnable()
        {
            if (m_GraphNodeModels == null)
                m_GraphNodeModels = new List<INodeModel>();
            m_NodesByGuid = new Dictionary<GUID, INodeModel>(m_GraphNodeModels.Count);

            foreach (var model in GetAllNodes())
            {
                if (model is null)
                    continue;
                ((NodeModel)model).AssetModel = AssetModel;
                model.PostGraphLoad();
                m_NodesByGuid.Add(model.Guid, model);
            }

            if (m_EdgeModels == null)
                m_EdgeModels = new List<EdgeModel>();
            if (m_StickyNoteModels == null)
                m_StickyNoteModels = new List<StickyNoteModel>();
            if (m_PlacematModels == null)
                m_PlacematModels = new List<PlacematModel>();
        }

        public void Dispose() {}

        public IEnumerable<IEdgeModel> GetEdgesConnections(IPortModel portModel)
        {
            return EdgeModels.Where(e => portModel.Direction == Direction.Input ? PortModel.Equivalent(e.InputPortModel, portModel) : PortModel.Equivalent(e.OutputPortModel, portModel));
        }

        public IEnumerable<IEdgeModel> GetEdgesConnections(INodeModel node)
        {
            return EdgeModels.Where(e => e.InputPortModel?.NodeModel.Guid == node.Guid
                || e.OutputPortModel?.NodeModel.Guid == node.Guid);
        }

        public IEnumerable<IEdgeModel> GetEdgesConnections(IEnumerable<IPortModel> portModels)
        {
            var models = new List<IEdgeModel>();
            foreach (var portModel in portModels)
            {
                models.AddRange(GetEdgesConnections(portModel));
            }

            return models;
        }

        public IEnumerable<IPortModel> GetConnections(IPortModel portModel)
        {
            return GetEdgesConnections(portModel).Select(e => portModel.Direction == Direction.Input ? e.OutputPortModel : e.InputPortModel)
                .Where(p => p != null);
        }

        public enum Verbosity
        {
            Errors,
            Verbose
        }

        public string FriendlyScriptName => TypeSystem.CodifyString(AssetModel.Name);

        public void DeleteStickyNotes(IStickyNoteModel[] stickyNotesToDelete)
        {
            foreach (IStickyNoteModel stickyNoteModel in stickyNotesToDelete)
                DeleteStickyNote(stickyNoteModel);
        }

        public void DeletePlacemats(IPlacematModel[] placematsToDelete)
        {
            foreach (IPlacematModel placematModel in placematsToDelete)
                DeletePlacemat(placematModel);
        }

        public void BypassNodes(INodeModel[] actionNodeModels)
        {
            foreach (var model in actionNodeModels)
            {
                var inputEdgeModels = GetEdgesConnections(model.InputsByDisplayOrder).ToList();
                var outputEdgeModels = GetEdgesConnections(model.OutputsByDisplayOrder).ToList();

                if (!inputEdgeModels.Any() || !outputEdgeModels.Any())
                    continue;

                DeleteEdges(inputEdgeModels);
                DeleteEdges(outputEdgeModels);

                CreateEdge(outputEdgeModels[0].InputPortModel, inputEdgeModels[0].OutputPortModel);
            }
        }

        public IEdgeModel EdgesConnectedToPorts(IPortModel input, IPortModel output)
        {
            return EdgeModels.FirstOrDefault(e => e.InputPortModel == input && e.OutputPortModel == output);
        }

        public IEnumerable<IEdgeModel> EdgesConnectedToPorts(IPortModel portModels)
        {
            return EdgeModels.Where(e => e.InputPortModel == portModels || e.OutputPortModel == portModels);
        }

        public void ResetChanges()
        {
            LastChanges = new GraphChangeList();
        }

        public void CleanUp()
        {
            m_GraphNodeModels.RemoveAll(n => n == null);
            m_StickyNoteModels.RemoveAll(s => s == null);
            m_PlacematModels.RemoveAll(p => p == null);
            DeleteEdges(m_EdgeModels.Where(e => !e.IsValid()));
            m_EdgeModels.RemoveAll(e => e == null);
        }

        public void UndoRedoPerformed()
        {
            OnEnable();
        }

        public virtual IEnumerable<INodeModel> GetAllNodes() => NodeModels;
    }
}
