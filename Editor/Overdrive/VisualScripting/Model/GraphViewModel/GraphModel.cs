using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.GraphToolsFoundation.Overdrive;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel
{
    [Serializable]
    public abstract class GraphModel : IGraphModel
    {
        [SerializeField]
        GraphAssetModel m_AssetModel;

        [SerializeReference]
        protected List<IGTFNodeModel> m_GraphNodeModels;
        [SerializeField]
        protected List<EdgeModel> m_EdgeModels;

        [SerializeField]
        protected List<StickyNoteModel> m_StickyNoteModels;

        [SerializeField]
        protected List<PlacematModel> m_PlacematModels;

        [SerializeReference]
        protected List<IGTFVariableDeclarationModel> m_GraphVariableModels = new List<IGTFVariableDeclarationModel>();

        [SerializeReference]
        protected List<IGTFVariableDeclarationModel> m_GraphPortalModels = new List<IGTFVariableDeclarationModel>();

        [SerializeReference]
        Stencil m_Stencil;

        public GraphChangeList LastChanges
        {
            get => m_LastChanges ?? (m_LastChanges = new GraphChangeList());
            private set => m_LastChanges = value;
        }

        IGraphChangeList IGTFGraphModel.LastChanges => LastChanges;

        protected GraphModel()
        {
            LastChanges = new GraphChangeList();
            if (m_GraphNodeModels == null)
                m_GraphNodeModels = new List<IGTFNodeModel>();
            if (m_EdgeModels == null)
                m_EdgeModels = new List<EdgeModel>();
            if (m_NodesByGuid == null)
                m_NodesByGuid = new Dictionary<GUID, IGTFNodeModel>();
            if (m_StickyNoteModels == null)
                m_StickyNoteModels = new List<StickyNoteModel>();
            if (m_PlacematModels == null)
                m_PlacematModels = new List<PlacematModel>();
        }

        public IGTFGraphAssetModel AssetModel
        {
            get => m_AssetModel;
            set => m_AssetModel = (GraphAssetModel)value;
        }

        Dictionary<GUID, IGTFNodeModel> m_NodesByGuid;

        [SerializeField]
        string name;
        public string Name { get => name; set => name = value; }

        GraphChangeList m_LastChanges;

        public List<IGTFPortModel> GetCompatiblePorts(IGTFPortModel startPortModel)
        {
            var startEdgePortalModel = startPortModel.NodeModel as IGTFEdgePortalModel;

            return this.GetPortModels().ToList().Where(pModel =>
            {
                if (startPortModel.PortDataType == typeof(ExecutionFlow) && pModel.PortDataType != typeof(ExecutionFlow))
                    return false;
                if (pModel.PortDataType == typeof(ExecutionFlow) && startPortModel.PortDataType != typeof(ExecutionFlow))
                    return false;

                // No good if ports belong to same node
                if (pModel == startPortModel ||
                    (pModel.NodeModel != null || startPortModel.NodeModel != null) &&
                    pModel.NodeModel == startPortModel.NodeModel)
                    return false;

                // No good if it's on the same portal either.
                if (pModel.NodeModel is IGTFEdgePortalModel edgePortalModel)
                {
                    if (edgePortalModel.DeclarationModel.Guid == startEdgePortalModel?.DeclarationModel.Guid)
                        return false;
                }

                // This is true for all ports
                if (pModel.Direction == startPortModel.Direction)
                    return false;

                // Last resort: same orientation required
                return pModel.Orientation == startPortModel.Orientation;
            })
                // deep in GraphView's EdgeDragHelper, this list is used to find the first port to use when dragging an
                // edge. as ports are returned in hierarchy order (back to front), in case of a conflict, the one behind
                // the others is returned. reverse the list to get the most logical one, the one on top of everything else
                .Reverse()
                .ToList();
        }

        public IReadOnlyDictionary<GUID, IGTFNodeModel> NodesByGuid => m_NodesByGuid ?? (m_NodesByGuid = new Dictionary<GUID, IGTFNodeModel>());

        public IReadOnlyList<IGTFNodeModel> NodeModels => m_GraphNodeModels;
        public IReadOnlyList<IGTFEdgeModel> EdgeModels => m_EdgeModels;
        public IReadOnlyList<IGTFStickyNoteModel> StickyNoteModels => m_StickyNoteModels;
        public IReadOnlyList<IGTFPlacematModel> PlacematModels => m_PlacematModels;
        public IList<IGTFVariableDeclarationModel> VariableDeclarations => m_GraphVariableModels;
        public IReadOnlyList<IGTFVariableDeclarationModel> PortalDeclarations => m_GraphPortalModels;

        public Stencil Stencil
        {
            get => m_Stencil;
            set => m_Stencil = value;
        }

        public string GetAssetPath()
        {
            return AssetDatabase.GetAssetPath(m_AssetModel);
        }

        public abstract string GetSourceFilePath();

        public TNodeType CreateNode<TNodeType>(string nodeName = "", Vector2 position = default, SpawnFlags spawnFlags = SpawnFlags.Default, Action<TNodeType> preDefineSetup = null, GUID? guid = null) where TNodeType : class, IGTFNodeModel
        {
            return (TNodeType)CreateNode(typeof(TNodeType), nodeName, position, spawnFlags, preDefineSetup == null ? (Action<IGTFNodeModel>)null : n => preDefineSetup.Invoke(n as TNodeType), guid);
        }

        public IGTFNodeModel CreateNode(Type nodeTypeToCreate, string nodeName, Vector2 position, SpawnFlags spawnFlags = SpawnFlags.Default, Action<IGTFNodeModel> preDefineSetup = null, GUID? guid = null)
        {
            return CreateNodeInternal(nodeTypeToCreate, nodeName, position, spawnFlags, preDefineSetup, guid);
        }

        public void MoveEdgeBefore(IGTFEdgeModel toMove, IGTFEdgeModel reference)
        {
            m_EdgeModels.Remove((EdgeModel)toMove);
            m_EdgeModels.Insert(m_EdgeModels.IndexOf((EdgeModel)reference), (EdgeModel)toMove);
        }

        public void MoveEdgeAfter(IGTFEdgeModel toMove, IGTFEdgeModel reference)
        {
            m_EdgeModels.Remove((EdgeModel)toMove);
            m_EdgeModels.Insert(m_EdgeModels.IndexOf((EdgeModel)reference) + 1, (EdgeModel)toMove);
        }

        internal IGTFNodeModel CreateNodeInternal(Type nodeTypeToCreate, string nodeName, Vector2 position,
            SpawnFlags spawnFlags = SpawnFlags.Default, Action<NodeModel> preDefineSetup = null, GUID? guid = null)
        {
            if (nodeTypeToCreate == null)
                throw new InvalidOperationException("Cannot create node with a null type");
            NodeModel nodeModel;
            if (typeof(IConstant).IsAssignableFrom(nodeTypeToCreate))
                nodeModel = new ConstantNodeModel {Value = (IConstant)Activator.CreateInstance(nodeTypeToCreate)};
            else
                nodeModel = (NodeModel)Activator.CreateInstance(nodeTypeToCreate);

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

        public virtual IGTFVariableNodeModel CreateVariableNode(IGTFVariableDeclarationModel declarationModel, Vector2 position, SpawnFlags spawnFlags = SpawnFlags.Default, GUID? guid = null)
        {
            if (declarationModel == null)
                return CreateNode<ThisNodeModel>("this", position, spawnFlags, null, guid);

            return CreateNode<VariableNodeModel>(declarationModel.DisplayTitle, position, spawnFlags, v => v.DeclarationModel = declarationModel, guid);
        }

        public IGTFConstantNodeModel CreateConstantNode(string constantName,
            TypeHandle constantTypeHandle, Vector2 position, SpawnFlags spawnFlags = SpawnFlags.Default, GUID? guid = null, Action<IGTFConstantNodeModel> preDefine = null)
        {
            var nodeType = Stencil.GetConstantNodeValueType(constantTypeHandle);

            void PreDefineSetup(IGTFNodeModel model)
            {
                if (model is ConstantNodeModel constantModel)
                {
                    constantModel.PredefineSetup();
                    preDefine?.Invoke(constantModel);
                }
            }

            return (ConstantNodeModel)CreateNode(nodeType, constantName, position, spawnFlags, PreDefineSetup, guid);
        }

        public IConstant CreateConstantValue(TypeHandle constantTypeHandle,
            Action<IConstant> preDefine = null)
        {
            var nodeType = Stencil.GetConstantNodeValueType(constantTypeHandle);
            var instance = (IConstant)Activator.CreateInstance(nodeType);
            instance.ObjectValue = instance.DefaultValue;
            preDefine?.Invoke(instance);
            return instance;
        }

        public void AddNode(IGTFNodeModel nodeModel)
        {
            Undo.RegisterCompleteObjectUndo(m_AssetModel, "Add Node");
            AddNodeInternal(nodeModel);
            LastChanges?.ChangedElements.Add(nodeModel);
        }

        public void AddNodeNoUndo(IGTFNodeModel nodeModel)
        {
            EditorUtility.SetDirty((Object)AssetModel);
            AddNodeInternal(nodeModel);
            LastChanges?.ChangedElements.Add(nodeModel);
        }

        void AddNodeInternal(IGTFNodeModel nodeModel)
        {
            ((NodeModel)nodeModel).AssetModel = AssetModel;
            m_GraphNodeModels.Add(nodeModel);
            if (m_NodesByGuid == null)
                m_NodesByGuid = new Dictionary<GUID, IGTFNodeModel>();
            m_NodesByGuid.Add(nodeModel.Guid, nodeModel);
        }

        public void DeleteNodes(IReadOnlyCollection<IGTFNodeModel> nodesToDelete, DeleteConnections deleteConnections)
        {
            foreach (var node in nodesToDelete)
                DeleteNode(node, deleteConnections);
        }

        public void DeleteNode(IGTFNodeModel nodeModel, DeleteConnections deleteConnections)
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

        internal void RegisterNodeGuid(IGTFNodeModel model)
        {
            m_NodesByGuid.Add(model.Guid, model);
        }

        internal void UnregisterNodeGuid(GUID nodeModelGuid)
        {
            m_NodesByGuid.Remove(nodeModelGuid);
        }

        internal void MoveNode(IGTFNodeModel nodeToMove, Vector2 newPosition)
        {
            var nodeModel = (NodeModel)nodeToMove;
            Undo.RegisterCompleteObjectUndo(m_AssetModel, "Move");
            nodeModel.Move(newPosition);
        }

        public IGTFEdgeModel CreateEdge(IGTFPortModel inputPort, IGTFPortModel outputPort)
        {
            var existing = EdgesConnectedToPorts(inputPort, outputPort);
            if (existing != null)
                return existing as IEdgeModel;

            var edgeModel = CreateOrphanEdge(inputPort, outputPort);
            AddEdge(edgeModel, inputPort, outputPort);

            return edgeModel;
        }

        public IGTFEdgeModel CreateOrphanEdge(IGTFPortModel input, IGTFPortModel output)
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

        void AddEdge(IGTFEdgeModel edgeModel, IGTFPortModel inputPort, IGTFPortModel outputPort)
        {
            Undo.RegisterCompleteObjectUndo(m_AssetModel, "Add Edge");
            ((EdgeModel)edgeModel).GraphModel = this;
            m_EdgeModels.Add((EdgeModel)edgeModel);
            LastChanges?.ChangedElements.Add(edgeModel);
            LastChanges?.ChangedElements.Add(inputPort.NodeModel);
            LastChanges?.ChangedElements.Add(outputPort.NodeModel);
        }

        public void DeleteEdge(IGTFPortModel input, IGTFPortModel output)
        {
            DeleteEdges(m_EdgeModels.Where(x => x.ToPort == input && x.FromPort == output));
        }

        public void DeleteEdge(IGTFEdgeModel gtfEdgeModel)
        {
            var edgeModel = gtfEdgeModel as IEdgeModel;

            Undo.RegisterCompleteObjectUndo(m_AssetModel, "Delete Edge");
            var model = (EdgeModel)edgeModel;

            edgeModel?.ToPort?.NodeModel?.OnDisconnection(edgeModel.ToPort, edgeModel.FromPort);
            edgeModel?.FromPort?.NodeModel?.OnDisconnection(edgeModel.FromPort, edgeModel.ToPort);

            LastChanges?.ChangedElements.Add(edgeModel?.ToPort?.NodeModel);
            LastChanges?.ChangedElements.Add(edgeModel?.FromPort?.NodeModel);

            m_EdgeModels.Remove(model);
            if (LastChanges != null)
            {
                LastChanges.DeletedEdges.Add(model);
                LastChanges.DeletedElements += 1;
            }
        }

        public void DeleteEdges(IEnumerable<IGTFEdgeModel> edgeModels)
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
                    case IGTFNodeModel nodeModel:
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

        public IGTFStickyNoteModel CreateStickyNote(Rect position, SpawnFlags dataSpawnFlags = SpawnFlags.Default)
        {
            var stickyNodeModel = (StickyNoteModel)CreateOrphanStickyNote(position);
            if (!dataSpawnFlags.IsOrphan())
                AddStickyNote(stickyNodeModel);

            return stickyNodeModel;
        }

        IGTFStickyNoteModel CreateOrphanStickyNote(Rect position)
        {
            var stickyNodeModel = new StickyNoteModel();
            stickyNodeModel.PositionAndSize = position;
            stickyNodeModel.GraphModel = this;

            return stickyNodeModel;
        }

        void AddStickyNote(IGTFStickyNoteModel model)
        {
            var stickyNodeModel = (StickyNoteModel)model;

            Undo.RegisterCompleteObjectUndo(m_AssetModel, "Add Sticky Note");
            LastChanges?.ChangedElements.Add(stickyNodeModel);
            stickyNodeModel.GraphModel = this;
            m_StickyNoteModels.Add(stickyNodeModel);
        }

        void DeleteStickyNote(IGTFStickyNoteModel stickyNoteModel)
        {
            Undo.RegisterCompleteObjectUndo(m_AssetModel, "Delete Sticky Note");
            var model = (StickyNoteModel)stickyNoteModel;

            m_StickyNoteModels.Remove(model);
            if (LastChanges != null)
                LastChanges.DeletedElements += 1;

            model.Destroy();
        }

        const string k_DefaultPlacematName = "Placemat";
        public IGTFPlacematModel CreatePlacemat(string title, Rect position, SpawnFlags dataSpawnFlags = SpawnFlags.Default)
        {
            var placematModel = (PlacematModel)CreateOrphanPlacemat(title ?? k_DefaultPlacematName, position);
            if (!dataSpawnFlags.IsOrphan())
                AddPlacemat(placematModel);

            return placematModel;
        }

        IGTFPlacematModel CreateOrphanPlacemat(string title, Rect position)
        {
            var placematModel = new PlacematModel();
            placematModel.Title = title;
            placematModel.PositionAndSize = position;
            placematModel.GraphModel = this;
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

        void AddPlacemat(IGTFPlacematModel model)
        {
            var placematModel = (PlacematModel)model;

            Undo.RegisterCompleteObjectUndo(m_AssetModel, "Add Placemat");
            LastChanges?.ChangedElements.Add(placematModel);
            placematModel.GraphModel = this;
            m_PlacematModels.Add(placematModel);
        }

        void DeletePlacemat(IGTFPlacematModel placematModel)
        {
            Undo.RegisterCompleteObjectUndo(m_AssetModel, "Delete Placemat");
            var model = (PlacematModel)placematModel;

            m_PlacematModels.Remove(model);
            if (LastChanges != null)
            {
                LastChanges.ChangedElements.AddRange(placematModel.HiddenElements);
                LastChanges.DeletedElements += 1;
            }

            model.Destroy();
        }

        public IGTFVariableDeclarationModel CreateGraphVariableDeclaration(string variableName,
            TypeHandle variableDataType, ModifierFlags modifierFlags, bool isExposed,
            IConstant initializationModel = null, GUID? guid = null)
        {
            var field = VariableDeclarationModel.Create(variableName, variableDataType, isExposed, this, VariableType.GraphVariable, modifierFlags, initializationModel, guid);
            Undo.RegisterCompleteObjectUndo((Object)AssetModel, "Create Graph Variable");
            VariableDeclarations.Add(field);
            return field;
        }

        public void DeleteVariableDeclarations(IEnumerable<IGTFVariableDeclarationModel> variableModels, bool deleteUsages, bool registerUndo)
        {
            if (registerUndo)
                Undo.RegisterCompleteObjectUndo((Object)AssetModel, "Remove Variable Declarations");

            foreach (var variableModel in variableModels)
            {
                if (LastChanges != null)
                {
                    LastChanges.BlackBoardChanged = true;
                }
                if (variableModel.VariableType == VariableType.GraphVariable || variableModel.VariableType == VariableType.ComponentQueryField)
                {
                    VariableDeclarations.Remove(variableModel);
                }
                if (deleteUsages)
                {
                    var nodesToDelete = this.FindReferencesInGraph(variableModel).Cast<IGTFNodeModel>().ToList();
                    DeleteNodes(nodesToDelete, DeleteConnections.True);
                }
            }
        }

        public IGTFVariableDeclarationModel CreateGraphPortalDeclaration(string portalName)
        {
            var field = VariableDeclarationModel.Create(portalName, TypeHandle.Unknown, false, this, VariableType.EdgePortal, ModifierFlags.ReadWrite);
            Undo.RegisterCompleteObjectUndo((Object)AssetModel, "Create Graph Portal Declaration Model");
            m_GraphPortalModels.Add(field);
            return field;
        }

        static readonly Vector2 k_PortalOffset = Vector2.right * 150;

        public IGTFEdgePortalModel CreateOppositePortal(IGTFEdgePortalModel edgePortalModel, SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            var offset = Vector2.zero;
            switch (edgePortalModel)
            {
                case IGTFEdgePortalEntryModel _:
                    offset = k_PortalOffset;
                    break;
                case IGTFEdgePortalExitModel _:
                    offset = -k_PortalOffset;
                    break;
            }
            var currentPos = ((EdgePortalModel)edgePortalModel).Position;
            return CreateOppositePortal(edgePortalModel, currentPos + offset, spawnFlags);
        }

        public IGTFEdgePortalModel CreateOppositePortal(IGTFEdgePortalModel edgePortalModel, Vector2 position, SpawnFlags spawnFlags = SpawnFlags.Default)
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

        Dictionary<string, IGuidUpdate> m_OldToNewGuids;

        internal void AddGuidToUpdate(IGuidUpdate element, string oldGuid)
        {
            if (m_OldToNewGuids == null)
                m_OldToNewGuids = new Dictionary<string, IGuidUpdate>();


            if (oldGuid == "00000000000000000000000000000000")
                oldGuid = (-m_OldToNewGuids.Count).ToString();

            Debug.Assert(!m_OldToNewGuids.ContainsKey(oldGuid), element + " already owns " + oldGuid);
            m_OldToNewGuids[oldGuid] = element;
        }

        void UpdateGuids()
        {
            if (m_OldToNewGuids == null)
                return;

            // Generate missing GUIDs
            foreach (var model in m_OldToNewGuids)
            {
                model.Value.AssignGuid(model.Key);
            }

            // Update placemat hidden elements.
            foreach (var model in m_OldToNewGuids)
            {
                if (model.Value is PlacematModel placematModel)
                    placematModel.UpdateHiddenGuids(m_OldToNewGuids);
            }

            m_OldToNewGuids.Clear();
        }

        protected internal virtual void OnEnable()
        {
            UpdateGuids();

            if (m_GraphNodeModels == null)
                m_GraphNodeModels = new List<IGTFNodeModel>();
            m_NodesByGuid = new Dictionary<GUID, IGTFNodeModel>(m_GraphNodeModels.Count);

            foreach (var model in NodeModels)
            {
                if (model is null)
                    continue;
                ((NodeModel)model).AssetModel = AssetModel;
                Debug.Assert(!model.Guid.Empty());
                m_NodesByGuid.Add(model.Guid, model);
            }

            if (m_EdgeModels == null)
                m_EdgeModels = new List<EdgeModel>();
            if (m_StickyNoteModels == null)
                m_StickyNoteModels = new List<StickyNoteModel>();
            if (m_PlacematModels == null)
            {
                m_PlacematModels = new List<PlacematModel>();
            }
        }

        public void Dispose() {}

        public IEnumerable<IGTFEdgeModel> GetEdgesConnections(IGTFPortModel portModel)
        {
            return EdgeModels.Where(e => portModel.Direction == Direction.Input ? PortModel.Equivalent(e.ToPort, portModel) : PortModel.Equivalent(e.FromPort, portModel));
        }

        public IEnumerable<IGTFEdgeModel> GetEdgesConnections(IGTFNodeModel node)
        {
            return EdgeModels.Where(e => e.ToPort?.NodeModel.Guid == node.Guid
                || e.FromPort?.NodeModel.Guid == node.Guid);
        }

        public IEnumerable<IGTFPortModel> GetConnections(IGTFPortModel portModel)
        {
            return GetEdgesConnections(portModel)
                .Select(e => portModel.Direction == Direction.Input ? e.FromPort : e.ToPort)
                .Where(p => p != null);
        }

        public string FriendlyScriptName => StringExtensions.CodifyString(AssetModel.Name);
        public string TypeName => StringExtensions.CodifyString(AssetModel.Name);

        public void DeleteStickyNotes(IGTFStickyNoteModel[] stickyNotesToDelete)
        {
            foreach (IGTFStickyNoteModel stickyNoteModel in stickyNotesToDelete)
                DeleteStickyNote(stickyNoteModel);
        }

        public void DeletePlacemats(IGTFPlacematModel[] placematsToDelete)
        {
            foreach (var placematModel in placematsToDelete)
                DeletePlacemat(placematModel);
        }

        public IGTFEdgeModel EdgesConnectedToPorts(IGTFPortModel input, IGTFPortModel output)
        {
            return EdgeModels.FirstOrDefault(e => e.ToPort == input && e.FromPort == output);
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
            DeleteEdges(m_EdgeModels.Where(e => e.ToPort == null || e.FromPort == null));
            m_EdgeModels.RemoveAll(e => e == null);
        }

        public void UndoRedoPerformed()
        {
            OnEnable();
        }

        public IGTFNodeModel DuplicateNode(IGTFNodeModel copiedNode, Dictionary<IGTFNodeModel, IGTFNodeModel> mapping, Vector2 delta)
        {
            var pastedNodeModel = (copiedNode as NodeModel).Clone();

            // Set graphmodel BEFORE define node as it is commonly use during Define
            pastedNodeModel.AssetModel = AssetModel;
            pastedNodeModel.Title = (copiedNode as IHasTitle)?.Title ?? "";
            pastedNodeModel.AssignNewGuid();
            pastedNodeModel.DefineNode();
            pastedNodeModel.ReinstantiateInputConstants();
            mapping.Add(copiedNode, pastedNodeModel);

            AddNode(pastedNodeModel);
            pastedNodeModel.Position += delta;

            return pastedNodeModel;
        }

        public List<IGTFVariableDeclarationModel> DuplicateGraphVariableDeclarations(List<IGTFVariableDeclarationModel> variableDeclarationModels)
        {
            List<IGTFVariableDeclarationModel> duplicatedModels = new List<IGTFVariableDeclarationModel>();
            foreach (var original in variableDeclarationModels)
            {
                if (original.VariableType != VariableType.GraphVariable)
                    continue;
                string uniqueName = original.Title;
                VariableDeclarationModel copy = ((VariableDeclarationModel)original).Clone();
                copy.Title = uniqueName;
                if (copy.InitializationModel != null)
                {
                    copy.CreateInitializationValue();
                    copy.InitializationModel.ObjectValue = original.InitializationModel.ObjectValue;
                }

                EditorUtility.SetDirty((Object)AssetModel);

                duplicatedModels.Add(copy);
                LastChanges.ChangedElements.Add(copy);
            }

            Undo.RegisterCompleteObjectUndo((Object)AssetModel, "Create Graph Variables");
            (VariableDeclarations as List<IGTFVariableDeclarationModel>)?.AddRange(duplicatedModels);

            return duplicatedModels;
        }

        public void ReorderGraphVariableDeclaration(IGTFVariableDeclarationModel variableDeclarationModel, int index)
        {
            Assert.IsTrue(index >= 0);

            Undo.RegisterCompleteObjectUndo((Object)AssetModel, "Reorder Graph Variable Declaration");

            var varDeclarationModel = (VariableDeclarationModel)variableDeclarationModel;
            if (varDeclarationModel.VariableType == VariableType.GraphVariable)
            {
                var oldIndex = VariableDeclarations.IndexOf(varDeclarationModel);
                VariableDeclarations.RemoveAt(oldIndex);
                if (index > oldIndex) index--;    // the actual index could have shifted due to the removal
                if (index >= VariableDeclarations.Count)
                    VariableDeclarations.Add(varDeclarationModel);
                else
                    VariableDeclarations.Insert(index, varDeclarationModel);
                LastChanges.ChangedElements.Add(variableDeclarationModel);
                LastChanges.DeletedElements++;
            }
        }

        public bool CheckIntegrity(Verbosity errors)
        {
            Assert.IsTrue((Object)AssetModel, "graph asset is invalid");
            bool failed = false;
            for (var i = 0; i < m_EdgeModels.Count; i++)
            {
                var edge = m_EdgeModels[i];
                if (edge.ToPort == null)
                {
                    failed = true;
                    Debug.Log($"Edge {i} input is null, output: {edge.FromPort}");
                }

                if (edge.FromPort == null)
                {
                    failed = true;
                    Debug.Log($"Edge {i} output is null, input: {edge.ToPort}");
                }
            }

            CheckNodeList(m_GraphNodeModels);
            if (!failed && errors == Verbosity.Verbose)
                Debug.Log("Integrity check succeeded");
            return !failed;
        }

        void CheckNodeList(IList<IGTFNodeModel> nodeModels, Dictionary<GUID, int> existingGuids = null)
        {
            if (existingGuids == null)
                existingGuids = new Dictionary<GUID, int>(nodeModels.Count * 4); // wild guess of total number of nodes, including stacked nodes
            for (var i = 0; i < nodeModels.Count; i++)
            {
                IGTFNodeModel node = nodeModels[i];

                Assert.IsTrue(node.GraphModel != null, $"Node {i} {node} graph is null");
                Assert.IsTrue(node.AssetModel != null, $"Node {i} {node} asset is null");
                Assert.IsNotNull(node, $"Node {i} is null");
                Assert.IsTrue(AssetModel.GetHashCode() == node.AssetModel?.GetHashCode(), $"Node {i} asset is not matching its actual asset");
                Assert.IsFalse(node.Guid.Empty(), $"Node {i} ({node.GetType()}) has an empty Guid");
                Assert.IsFalse(existingGuids.TryGetValue(node.Guid, out var oldIndex), $"duplicate GUIDs: Node {i} ({node.GetType()}) and Node {oldIndex} have the same guid {node.Guid}");
                existingGuids.Add(node.Guid, i);

                if (node.Destroyed)
                    continue;
                CheckNodePorts(node.InputsById);
                CheckNodePorts(node.OutputsById);

                if (node is VariableNodeModel variableNode && variableNode.DeclarationModel != null)
                {
                    if (variableNode.VariableDeclarationModel.VariableType == VariableType.GraphVariable)
                    {
                        var originalDeclarations = VariableDeclarations.Where(d => d.Guid == variableNode.DeclarationModel.Guid);
                        Assert.IsTrue(originalDeclarations.Count() <= 1);
                        var originalDeclaration = originalDeclarations.SingleOrDefault();
                        Assert.IsNotNull(originalDeclaration, $"Variable Node {i} {variableNode.Title} has a declaration model, but it was not present in the graph's variable declaration list");
                        Assert.IsTrue(ReferenceEquals(originalDeclaration, variableNode.DeclarationModel), $"Variable Node {i} {variableNode.Title} has a declaration model that was not ReferenceEquals() to the matching one in the graph");
                    }
                }
            }
        }

        static void CheckNodePorts(IReadOnlyDictionary<string, IGTFPortModel> portsById)
        {
            foreach (var kv in portsById)
            {
                string portId = portsById[kv.Key].UniqueName;
                Assert.AreEqual(kv.Key, portId, $"Node {kv.Key} port and its actual id {portId} mismatch");
            }
        }

        public void QuickCleanup()
        {
            for (var i = m_EdgeModels.Count - 1; i >= 0; i--)
            {
                var edge = m_EdgeModels[i];
                if (edge?.ToPort == null || edge.FromPort == null)
                    m_EdgeModels.RemoveAt(i);
            }

            CleanupNodes(m_GraphNodeModels);
        }

        static void CleanupNodes(IList<IGTFNodeModel> models)
        {
            for (var i = models.Count - 1; i >= 0; i--)
            {
                if (models[i].Destroyed)
                    models.RemoveAt(i);
            }
        }

        public abstract CompilationResult Compile(ITranslator translator);
    }
}
