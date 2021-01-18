using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    [Serializable]
    public abstract class GraphModel : IGraphModel, ISerializationCallbackReceiver
    {
        [SerializeReference]
        GraphAssetModel m_AssetModel;

        [SerializeReference]
        protected List<INodeModel> m_GraphNodeModels;

        [SerializeField, Obsolete]
        // ReSharper disable once Unity.RedundantFormerlySerializedAsAttribute
        List<EdgeModel> m_EdgeModels;

        [SerializeReference]
        List<IBadgeModel> m_BadgeModels;

        [SerializeReference]
        protected List<IEdgeModel> m_GraphEdgeModels;

        [SerializeReference, Obsolete]
        List<EdgeModel> m_PolymorphicEdgeModels;

        [SerializeField, Obsolete]
        List<StickyNoteModel> m_StickyNoteModels;

        [SerializeReference]
        protected List<IStickyNoteModel> m_GraphStickyNoteModels;

        [SerializeField, Obsolete]
        List<PlacematModel> m_PlacematModels;

        [SerializeReference]
        protected List<IPlacematModel> m_GraphPlacematModels;

        [SerializeReference]
        protected List<IVariableDeclarationModel> m_GraphVariableModels;

        [SerializeReference]
        protected List<IDeclarationModel> m_GraphPortalModels;

        [SerializeField]
        private string m_StencilTypeName; // serialized as string, resolved as type by ISerializationCallbackReceiver

        public virtual Type DefaultStencilType => null;

        // kept for backward compatibility, Stencil won't be serializable in the future
        [SerializeReference, FormerlySerializedAs("m_Stencil")]
        Stencil m_SerializedStencil;

        protected Stencil OldSerializedStencil => m_SerializedStencil;

        Type m_StencilType;

        public Type StencilType
        {
            get => m_StencilType;
            set
            {
                if (value == null)
                    value = DefaultStencilType;
                Assert.IsTrue(typeof(Stencil).IsAssignableFrom(value));
                m_StencilType = value;
                Stencil = InstantiateStencil(StencilType);
            }
        }

        public Stencil Stencil { get; private set; }

        [SerializeField, FormerlySerializedAs("name")]
        string m_Name;

        Dictionary<GUID, INodeModel> m_NodesByGuid;

        public IGraphAssetModel AssetModel
        {
            get => m_AssetModel;
            set => m_AssetModel = (GraphAssetModel)value;
        }

        public IReadOnlyList<INodeModel> NodeModels => m_GraphNodeModels;

        public IReadOnlyList<IEdgeModel> EdgeModels => m_GraphEdgeModels;

        public IReadOnlyList<IBadgeModel> BadgeModels => m_BadgeModels;

        public IReadOnlyList<IStickyNoteModel> StickyNoteModels => m_GraphStickyNoteModels;

        public IReadOnlyList<IPlacematModel> PlacematModels => m_GraphPlacematModels;

        public IReadOnlyList<IVariableDeclarationModel> VariableDeclarations => m_GraphVariableModels;

        public IReadOnlyList<IDeclarationModel> PortalDeclarations => m_GraphPortalModels;

        public IReadOnlyDictionary<GUID, INodeModel> NodesByGuid => m_NodesByGuid ?? (m_NodesByGuid = new Dictionary<GUID, INodeModel>());

        Dictionary<string, IGuidUpdate> m_OldToNewGuids;

        public string Name
        {
            get => m_Name;
            set => m_Name = value;
        }

        protected GraphModel()
        {
            m_GraphNodeModels = new List<INodeModel>();
            m_GraphEdgeModels = new List<IEdgeModel>();
            m_BadgeModels = new List<IBadgeModel>();
            m_GraphStickyNoteModels = new List<IStickyNoteModel>();
            m_GraphPlacematModels = new List<IPlacematModel>();
            m_GraphVariableModels = new List<IVariableDeclarationModel>();
            m_GraphPortalModels = new List<IDeclarationModel>();
        }

        protected virtual bool IsCompatiblePort(IPortModel startPortModel, IPortModel compatiblePortModel)
        {
            var startEdgePortalModel = startPortModel.NodeModel as IEdgePortalModel;

            if (startPortModel.PortDataType == typeof(ExecutionFlow) && compatiblePortModel.PortDataType != typeof(ExecutionFlow))
                return false;
            if (compatiblePortModel.PortDataType == typeof(ExecutionFlow) && startPortModel.PortDataType != typeof(ExecutionFlow))
                return false;

            // No good if ports belong to same node that does not allow self connect
            if (compatiblePortModel == startPortModel ||
                (compatiblePortModel.NodeModel != null || startPortModel.NodeModel != null) &&
                !startPortModel.NodeModel.AllowSelfConnect && compatiblePortModel.NodeModel == startPortModel.NodeModel)
                return false;

            // No good if it's on the same portal either.
            if (compatiblePortModel.NodeModel is IEdgePortalModel edgePortalModel)
            {
                if (edgePortalModel.DeclarationModel.Guid == startEdgePortalModel?.DeclarationModel.Guid)
                    return false;
            }

            // This is true for all ports
            return compatiblePortModel.Direction != startPortModel.Direction;
        }

        public virtual List<IPortModel> GetCompatiblePorts(IPortModel startPortModel)
        {
            return this.GetPortModels().ToList().Where(pModel =>
            {
                return IsCompatiblePort(startPortModel, pModel);
            })
                // deep in GraphView's EdgeDragHelper, this list is used to find the first port to use when dragging an
                // edge. as ports are returned in hierarchy order (back to front), in case of a conflict, the one behind
                // the others is returned. reverse the list to get the most logical one, the one on top of everything else
                .Reverse()
                .ToList();
        }

        public INodeModel CreateNode(Type nodeTypeToCreate, string nodeName, Vector2 position,
            GUID? guid = null, Action<INodeModel> preDefineSetup = null, SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            return CreateNodeInternal(nodeTypeToCreate, nodeName, position, spawnFlags, preDefineSetup, guid);
        }

        protected virtual INodeModel CreateNodeInternal(Type nodeTypeToCreate, string nodeName, Vector2 position,
            SpawnFlags spawnFlags = SpawnFlags.Default, Action<INodeModel> preDefineSetup = null, GUID? guid = null)
        {
            if (nodeTypeToCreate == null)
                throw new ArgumentNullException(nameof(nodeTypeToCreate));
            INodeModel nodeModel;
            if (typeof(IConstant).IsAssignableFrom(nodeTypeToCreate))
                nodeModel = new ConstantNodeModel {Value = (IConstant)Activator.CreateInstance(nodeTypeToCreate)};
            else if (typeof(INodeModel).IsAssignableFrom(nodeTypeToCreate))
                nodeModel = (INodeModel)Activator.CreateInstance(nodeTypeToCreate);
            else
                throw new ArgumentOutOfRangeException(nameof(nodeTypeToCreate));

            if (nodeModel is IHasTitle titled)
                titled.Title = nodeName ?? nodeTypeToCreate.Name;

            nodeModel.Position = position;
            nodeModel.Guid = guid ?? GUID.Generate();
            nodeModel.AssetModel = AssetModel;
            preDefineSetup?.Invoke(nodeModel);
            nodeModel.DefineNode();
            if (!spawnFlags.IsOrphan())
            {
                AddNode(nodeModel);
                if (m_AssetModel)
                    EditorUtility.SetDirty(m_AssetModel);
            }
            return nodeModel;
        }

        public virtual IVariableNodeModel CreateVariableNode(IVariableDeclarationModel declarationModel,
            Vector2 position, GUID? guid = null, SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            return this.CreateNode<VariableNodeModel>(declarationModel.DisplayTitle, position, guid, v => v.DeclarationModel = declarationModel, spawnFlags);
        }

        public virtual IConstantNodeModel CreateConstantNode(TypeHandle constantTypeHandle, string constantName,
            Vector2 position, GUID? guid = null, Action<IConstantNodeModel> preDefine = null, SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            var nodeType = Stencil.GetConstantNodeValueType(constantTypeHandle);

            void PreDefineSetup(INodeModel model)
            {
                if (model is IConstantNodeModel constantModel)
                {
                    constantModel.PredefineSetup();
                    preDefine?.Invoke(constantModel);
                }
            }

            return (IConstantNodeModel)CreateNode(nodeType, constantName, position, guid, PreDefineSetup, spawnFlags);
        }

        public void AddBadge(IBadgeModel badgeModel)
        {
            m_BadgeModels.Add(badgeModel);
        }

        public void RemoveBadge(IBadgeModel badgeModel)
        {
            m_BadgeModels.Remove(badgeModel);
        }

        public IReadOnlyCollection<IGraphElementModel> DeleteBadges()
        {
            var deletedBadges = new List<IGraphElementModel>(m_BadgeModels);
            m_BadgeModels.Clear();
            return deletedBadges;
        }

        public IReadOnlyCollection<IGraphElementModel> DeleteBadgesOfType<T>() where T : IBadgeModel
        {
            var deletedBadges = m_BadgeModels
                .Where(b => b is T)
                .ToList();

            m_BadgeModels = m_BadgeModels
                .Where(b => !(b is T))
                .ToList();

            return deletedBadges;
        }

        public void AddNode(INodeModel nodeModel)
        {
            AddNodeInternal(nodeModel);
        }

        void AddNodeInternal(INodeModel nodeModel)
        {
            nodeModel.AssetModel = AssetModel;
            m_GraphNodeModels.Add(nodeModel);
            if (m_NodesByGuid == null)
                m_NodesByGuid = new Dictionary<GUID, INodeModel>();
            m_NodesByGuid.Add(nodeModel.Guid, nodeModel);
        }

        public INodeModel DuplicateNode(INodeModel sourceNode, Vector2 delta)
        {
            var pastedNodeModel = sourceNode.Clone();

            // Set graphmodel BEFORE define node as it is commonly use during Define
            pastedNodeModel.AssetModel = AssetModel;
            pastedNodeModel.AssignNewGuid();
            pastedNodeModel.DefineNode();
            pastedNodeModel.OnDuplicateNode(sourceNode);

            AddNode(pastedNodeModel);
            pastedNodeModel.Position += delta;

            return pastedNodeModel;
        }

        public virtual IEdgeModel DuplicateEdge(IEdgeModel sourceEdge, INodeModel targetInputNode, INodeModel targetOutputNode)
        {
            IPortModel inputPortModel = null;
            IPortModel outputPortModel = null;
            if (targetInputNode != null && targetOutputNode != null)
            {
                // Both node were duplicated; create a new edge between the duplicated nodes.
                inputPortModel = (targetInputNode as IInOutPortsNode)?.InputsById[sourceEdge.ToPortId];
                outputPortModel = (targetOutputNode as IInOutPortsNode)?.OutputsById[sourceEdge.FromPortId];
            }
            else if (targetInputNode != null)
            {
                inputPortModel = (targetInputNode as IInOutPortsNode)?.InputsById[sourceEdge.ToPortId];
                outputPortModel = sourceEdge.FromPort;
            }
            else if (targetOutputNode != null)
            {
                inputPortModel = sourceEdge.ToPort;
                outputPortModel = (targetOutputNode as IInOutPortsNode)?.OutputsById[sourceEdge.FromPortId];
            }

            if (inputPortModel != null && outputPortModel != null)
            {
                if (inputPortModel.Capacity == PortCapacity.Single && inputPortModel.GetConnectedEdges().Any())
                    return null;
                if (outputPortModel.Capacity == PortCapacity.Single && outputPortModel.GetConnectedEdges().Any())
                    return null;

                return CreateEdge(inputPortModel, outputPortModel);
            }

            return null;
        }

        public virtual void CreateItemizedNode(State state, int nodeOffset, ref IPortModel outputPortModel)
        {
            if (!outputPortModel.IsConnected())
                return;

            if (outputPortModel.NodeModel is IConstantNodeModel || outputPortModel.NodeModel is IVariableNodeModel)
            {
                CreateItemizedNode(nodeOffset, ref outputPortModel);
            }
        }

        protected void CreateItemizedNode(int nodeOffset, ref IPortModel outputPortModel)
        {
            Vector2 offset = Vector2.up * nodeOffset;
            var nodeToConnect = DuplicateNode(outputPortModel.NodeModel, offset) as IInOutPortsNode;
            outputPortModel = nodeToConnect?.OutputsById[outputPortModel.UniqueName];
        }

        public IReadOnlyCollection<IGraphElementModel> DeleteNodes(IReadOnlyCollection<INodeModel> nodeModels, bool deleteConnections)
        {
            var deletedModels = new List<IGraphElementModel>();

            foreach (var nodeModel in nodeModels.Where(n => n.IsDeletable()))
            {
                m_GraphNodeModels.Remove(nodeModel);
                deletedModels.Add(nodeModel);

                if (deleteConnections)
                {
                    var connectedEdges = nodeModel.GetConnectedEdges().ToList();
                    deletedModels.AddRange(DeleteEdges(connectedEdges));
                }
                m_NodesByGuid?.Remove(nodeModel.Guid);

                // If this is the last portal with the given declaration, delete the declaration.
                if (nodeModel is EdgePortalModel edgePortalModel &&
                    !this.FindReferencesInGraph<IEdgePortalModel>(edgePortalModel.DeclarationModel).Any())
                {
                    m_GraphPortalModels.Remove(edgePortalModel.DeclarationModel);
                    deletedModels.Add(edgePortalModel.DeclarationModel);
                }

                nodeModel.Destroy();
            }

            return deletedModels;
        }

        public virtual IEdgeModel CreateEdge(IPortModel toPort, IPortModel fromPort)
        {
            return CreateEdge<EdgeModel>(toPort, fromPort);
        }

        public EdgeT CreateEdge<EdgeT>(IPortModel toPort, IPortModel fromPort) where EdgeT : IEdgeModel, new()
        {
            var existing = this.GetEdgeConnectedToPorts(toPort, fromPort);
            if (existing != null)
                return (EdgeT)existing;

            var edgeModel = CreateOrphanEdge<EdgeT>(toPort, fromPort);
            if (edgeModel != null)
            {
                m_GraphEdgeModels.Add(edgeModel);
            }

            return edgeModel;
        }

        protected EdgeT CreateOrphanEdge<EdgeT>(IPortModel toPort, IPortModel fromPort) where EdgeT : IEdgeModel, new()
        {
            Assert.IsNotNull(toPort);
            Assert.IsNotNull(toPort.NodeModel);
            Assert.IsNotNull(fromPort);
            Assert.IsNotNull(fromPort.NodeModel);

            var edgeModel = new EdgeT { AssetModel = AssetModel, EdgeLabel = "" };
            edgeModel.SetPorts(toPort, fromPort);

            toPort.NodeModel.OnConnection(toPort, fromPort);
            fromPort.NodeModel.OnConnection(fromPort, toPort);

            return edgeModel;
        }

        public IReadOnlyCollection<IGraphElementModel> DeleteEdges(IReadOnlyCollection<IEdgeModel> edgeModels)
        {
            var deletedModels = new List<IGraphElementModel>();

            foreach (var edgeModel in edgeModels.Where(e => e.IsDeletable()))
            {
                edgeModel?.ToPort?.NodeModel?.OnDisconnection(edgeModel.ToPort, edgeModel.FromPort);
                edgeModel?.FromPort?.NodeModel?.OnDisconnection(edgeModel.FromPort, edgeModel.ToPort);

                m_GraphEdgeModels.Remove(edgeModel);
                deletedModels.Add(edgeModel);
            }

            return deletedModels;
        }

        public virtual IStickyNoteModel CreateStickyNote(Rect position, SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            var stickyNodeModel = CreateOrphanStickyNote<StickyNoteModel>(position);
            if (!spawnFlags.IsOrphan())
            {
                m_GraphStickyNoteModels.Add(stickyNodeModel);
            }
            return stickyNodeModel;
        }

        protected StickyNoteT CreateOrphanStickyNote<StickyNoteT>(Rect position) where StickyNoteT : IStickyNoteModel, new()
        {
            var stickyNodeModel = new StickyNoteT
            {
                PositionAndSize = position,
                AssetModel = AssetModel
            };

            return stickyNodeModel;
        }

        public IReadOnlyCollection<IGraphElementModel> DeleteStickyNotes(IReadOnlyCollection<IStickyNoteModel> stickyNoteModels)
        {
            var deletedModels = new List<IGraphElementModel>();

            foreach (var stickyNoteModel in stickyNoteModels.Where(s => s.IsDeletable()))
            {
                m_GraphStickyNoteModels.Remove(stickyNoteModel);
                stickyNoteModel.Destroy();
                deletedModels.Add(stickyNoteModel);
            }

            return deletedModels;
        }

        public virtual IPlacematModel CreatePlacemat(Rect position, SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            var placematModel = CreateOrphanPlacemat<PlacematModel>(position);
            if (!spawnFlags.IsOrphan())
                AddPlacemat(placematModel);

            return placematModel;
        }

        PlacematT CreateOrphanPlacemat<PlacematT>(Rect position) where PlacematT : IPlacematModel, new()
        {
            var placematModel = new PlacematT
            {
                PositionAndSize = position,
                AssetModel = AssetModel,
                ZOrder = GetPlacematTopZOrder()
            };
            return placematModel;
        }

        int GetPlacematTopZOrder()
        {
            int maxZ = Int32.MinValue;
            foreach (var model in PlacematModels)
            {
                maxZ = Math.Max(model.ZOrder, maxZ);
            }
            return maxZ == Int32.MinValue ? 1 : maxZ + 1;
        }

        void AddPlacemat(PlacematModel placematModel)
        {
            m_GraphPlacematModels.Add(placematModel);
        }

        public IReadOnlyCollection<IGraphElementModel> DeletePlacemats(IReadOnlyCollection<IPlacematModel> placematModels)
        {
            var deletedModels = new List<IGraphElementModel>();

            foreach (var placematModel in placematModels.Where(p => p.IsDeletable()))
            {
                m_GraphPlacematModels.Remove(placematModel);
                placematModel.Destroy();
                deletedModels.Add(placematModel);
            }

            return deletedModels;
        }

        private Stencil InstantiateStencil(Type stencilType)
        {
            Debug.Assert(typeof(Stencil).IsAssignableFrom(stencilType));
            var stencil = (Stencil)Activator.CreateInstance(stencilType);
            Assert.IsNotNull(stencil);
            stencil.GraphModel = this;
            return stencil;
        }

        public IVariableDeclarationModel CreateGraphVariableDeclaration(TypeHandle variableDataType, string variableName,
            ModifierFlags modifierFlags, bool isExposed, IConstant initializationModel = null, GUID? guid = null)
        {
            var field = VariableDeclarationModel.Create(variableName, variableDataType, isExposed, this,
                VariableType.GraphVariable, modifierFlags, initializationModel: initializationModel, guid: guid);
            m_GraphVariableModels.Add(field);
            return field;
        }

        public virtual IVariableDeclarationModel DuplicateGraphVariableDeclaration(IVariableDeclarationModel sourceModel)
        {
            if (sourceModel.VariableType != VariableType.GraphVariable)
                return null;
            string uniqueName = sourceModel.Title;
            VariableDeclarationModel copy = ((VariableDeclarationModel)sourceModel).Clone();
            copy.Title = uniqueName;
            if (copy.InitializationModel != null)
            {
                copy.CreateInitializationValue();
                copy.InitializationModel.ObjectValue = sourceModel.InitializationModel.ObjectValue;
            }

            EditorUtility.SetDirty((Object)AssetModel);

            m_GraphVariableModels.Add(copy);

            return copy;
        }

        public IReadOnlyCollection<IGraphElementModel> DeleteVariableDeclarations(IReadOnlyCollection<IVariableDeclarationModel> variableModels, bool deleteUsages)
        {
            var deletedModels = new List<IGraphElementModel>();

            foreach (var variableModel in variableModels.Where(v => v.IsDeletable()))
            {
                if (variableModel.VariableType == VariableType.GraphVariable)
                {
                    m_GraphVariableModels.Remove(variableModel);
                    deletedModels.Add(variableModel);
                }

                if (deleteUsages)
                {
                    var nodesToDelete = this.FindReferencesInGraph(variableModel).Cast<INodeModel>().ToList();
                    deletedModels.AddRange(DeleteNodes(nodesToDelete, deleteConnections: true));
                }
            }

            return deletedModels;
        }

        public IDeclarationModel CreateGraphPortalDeclaration(string portalName, SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            var field = new DeclarationModel {Title = portalName};
            field.AssignNewGuid();

            if (!spawnFlags.IsOrphan())
                m_GraphPortalModels.Add(field);

            return field;
        }

        public IEdgePortalModel CreateOppositePortal(IEdgePortalModel edgePortalModel, Vector2 position, SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            EdgePortalModel createdPortal = null;
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
                createdPortal = (EdgePortalModel)CreateNode(oppositeType, edgePortalModel.Title, position, spawnFlags: spawnFlags);

            if (createdPortal != null)
                createdPortal.DeclarationModel = edgePortalModel.DeclarationModel;

            return createdPortal;
        }

        public IEdgePortalEntryModel CreateEntryPortalFromEdge(IEdgeModel edgeModel)
        {
            var outputPortModel = edgeModel.FromPort;
            if (outputPortModel.PortType == PortType.Execution)
                return this.CreateNode<ExecutionEdgePortalEntryModel>();

            return this.CreateNode<DataEdgePortalEntryModel>();
        }

        public IEdgePortalExitModel CreateExitPortalFromEdge(IEdgeModel edgeModel)
        {
            var inputPortModel = edgeModel.ToPort;
            if (inputPortModel?.PortType == PortType.Execution)
                return this.CreateNode<ExecutionEdgePortalExitModel>();

            return this.CreateNode<DataEdgePortalExitModel>();
        }

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

        public virtual void OnEnable()
        {
            if (m_GraphEdgeModels == null)
                m_GraphEdgeModels = new List<IEdgeModel>();

            if (m_GraphStickyNoteModels == null)
                m_GraphStickyNoteModels = new List<IStickyNoteModel>();

            if (m_GraphPlacematModels == null)
                m_GraphPlacematModels = new List<IPlacematModel>();

#pragma warning disable 612
            // Serialized data conversion code
            if (m_EdgeModels?.Count > 0)
            {
                m_GraphEdgeModels.AddRange(m_EdgeModels);
                m_EdgeModels = null;
            }

            if (m_PolymorphicEdgeModels?.Count > 0)
            {
                m_GraphEdgeModels.AddRange(m_PolymorphicEdgeModels);
                m_PolymorphicEdgeModels = null;
            }

            // Serialized data conversion code
            if (m_StickyNoteModels != null)
            {
                m_GraphStickyNoteModels.AddRange(m_StickyNoteModels);
                m_StickyNoteModels = null;
            }

            // Serialized data conversion code
            if (m_PlacematModels != null)
            {
                m_GraphPlacematModels.AddRange(m_PlacematModels);
                m_PlacematModels = null;
            }
#pragma warning restore 612

            UpdateGuids();

            if (m_GraphNodeModels == null)
                m_GraphNodeModels = new List<INodeModel>();

            m_NodesByGuid = new Dictionary<GUID, INodeModel>(m_GraphNodeModels.Count);

            foreach (var model in NodeModels)
            {
                if (model is null)
                    continue;
                model.AssetModel = AssetModel;
                Debug.Assert(!model.Guid.Empty());
                m_NodesByGuid.Add(model.Guid, model);
            }

            MigrateNodes();

            // needed now that nodemodels are not in separate node assets that got OnEnable() before the graph itself would
            foreach (var nodeModel in NodeModels)
            {
                (nodeModel as NodeModel)?.DefineNode();
            }
        }

        protected virtual void MigrateNodes()
        {
        }

        public void OnDisable()
        {
        }

        public void Dispose() {}

        public void UndoRedoPerformed()
        {
            OnEnable();
        }

        public virtual void OnAfterDeserializeAssetModel()
        {
            if (OldSerializedStencil != null)
            {
                StencilType = OldSerializedStencil.GetType();
            }
            UpgradePortalModels();
        }

        // TODO: JOCE Remove before the GTF goes public. Should no longer en needed at that point.
        void UpgradePortalModels()
        {
            var oldPortalDeclarations = PortalDeclarations.OfType<VariableDeclarationModel>().ToList();
            foreach (var oldPortalDeclaration in oldPortalDeclarations)
            {
                var newPortalDeclaration = new DeclarationModel
                {
                    Title = oldPortalDeclaration.Title,
                    Guid = oldPortalDeclaration.Guid
                };

                var portalModels = NodeModels
                    .OfType<EdgePortalModel>()
                    .Where(v => v.DeclarationModel != null &&
                        v.DeclarationModel.GetType() == typeof(VariableDeclarationModel) &&
                        oldPortalDeclaration.Guid == v.DeclarationModel.Guid);
                foreach (var portalModel in portalModels)
                {
                    portalModel.DeclarationModel = newPortalDeclaration;
                }

                m_GraphPortalModels.Remove(oldPortalDeclaration);
                m_GraphPortalModels.Add(newPortalDeclaration);
            }
        }

        public virtual bool CheckIntegrity(Verbosity errors)
        {
            return GraphModelExtensions.CheckIntegrity(this, errors);
        }

        public void OnBeforeSerialize()
        {
            // Stencil shouldn't be serialized in graphmodels, this is only kept for backward compatibility with older graphs
            m_SerializedStencil = null;

            if (StencilType != null)
                m_StencilTypeName = StencilType.AssemblyQualifiedName;
        }

        public void OnAfterDeserialize()
        {
            if (!string.IsNullOrEmpty(m_StencilTypeName))
            {
                StencilType = Type.GetType(m_StencilTypeName) ?? DefaultStencilType;
            }
        }

        public virtual void CloneGraph(IGraphModel sourceGraphModel)
        {
            m_GraphNodeModels = new List<INodeModel>();
            m_GraphEdgeModels = new List<IEdgeModel>();
            m_GraphStickyNoteModels = new List<IStickyNoteModel>();
            m_GraphPlacematModels = new List<IPlacematModel>();
            m_GraphVariableModels = new List<IVariableDeclarationModel>();
            m_GraphPortalModels = new List<IDeclarationModel>();

            var elementMapping = new Dictionary<string, IGraphElementModel>();
            var nodeMapping = new Dictionary<INodeModel, INodeModel>();
            var variableMapping = new Dictionary<IVariableDeclarationModel, IVariableDeclarationModel>();

            if (sourceGraphModel.VariableDeclarations.Any())
            {
                List<IVariableDeclarationModel> variableDeclarationModels =
                    sourceGraphModel.VariableDeclarations.ToList();

                foreach (var sourceModel in variableDeclarationModels)
                {
                    var copy = DuplicateGraphVariableDeclaration(sourceModel);
                    variableMapping.Add(sourceModel, copy);
                }
            }

            foreach (var sourceNode in sourceGraphModel.NodeModels)
            {
                var pastedNode = DuplicateNode(sourceNode, Vector2.zero);
                nodeMapping[sourceNode] = pastedNode;
            }

            foreach (var nodeModel in nodeMapping)
            {
                elementMapping.Add(nodeModel.Key.Guid.ToString(), nodeModel.Value);
            }

            foreach (var sourceEdge in sourceGraphModel.EdgeModels)
            {
                elementMapping.TryGetValue(sourceEdge.ToNodeGuid.ToString(), out var newInput);
                elementMapping.TryGetValue(sourceEdge.FromNodeGuid.ToString(), out var newOutput);

                DuplicateEdge(sourceEdge, newInput as INodeModel, newOutput as INodeModel);
                if (sourceEdge != null)
                {
                    elementMapping.Add(sourceEdge.Guid.ToString(), sourceEdge);
                }
            }

            foreach (var sourceVariableNode in sourceGraphModel.NodeModels.Where(model => model is VariableNodeModel))
            {
                elementMapping.TryGetValue(sourceVariableNode.Guid.ToString(), out var newNode);

                ((VariableNodeModel)newNode).DeclarationModel =
                    variableMapping[((VariableNodeModel)sourceVariableNode).VariableDeclarationModel];
            }

            foreach (var stickyNote in sourceGraphModel.StickyNoteModels)
            {
                var newPosition = new Rect(stickyNote.PositionAndSize.position, stickyNote.PositionAndSize.size);
                var pastedStickyNote = (StickyNoteModel)CreateStickyNote(newPosition);
                pastedStickyNote.Title = stickyNote.Title;
                pastedStickyNote.Contents = stickyNote.Contents;
                pastedStickyNote.Theme = stickyNote.Theme;
                pastedStickyNote.TextSize = stickyNote.TextSize;
                elementMapping.Add(stickyNote.Guid.ToString(), pastedStickyNote);
            }

            List<PlacematModel> pastedPlacemats = new List<PlacematModel>();
            // Keep placemats relative order
            foreach (var placemat in sourceGraphModel.PlacematModels.OrderBy(p => p.ZOrder))
            {
                var newPosition = new Rect(placemat.PositionAndSize.position, placemat.PositionAndSize.size);
                var pastedPlacemat = (PlacematModel)CreatePlacemat(newPosition);
                pastedPlacemat.Title = placemat.Title;
                pastedPlacemat.Color = placemat.Color;
                pastedPlacemat.Collapsed = placemat.Collapsed;
                pastedPlacemat.HiddenElementsGuid = ((PlacematModel)placemat).HiddenElementsGuid;
                pastedPlacemats.Add(pastedPlacemat);
                elementMapping.Add(placemat.Guid.ToString(), pastedPlacemat);
            }

            // Update hidden content to new node ids.
            foreach (var pastedPlacemat in pastedPlacemats)
            {
                if (pastedPlacemat.Collapsed)
                {
                    List<string> pastedHiddenContent = new List<string>();
                    foreach (var guid in pastedPlacemat.HiddenElementsGuid)
                    {
                        IGraphElementModel pastedElement;
                        if (elementMapping.TryGetValue(guid, out pastedElement))
                        {
                            pastedHiddenContent.Add(pastedElement.Guid.ToString());
                        }
                    }

                    pastedPlacemat.HiddenElementsGuid = pastedHiddenContent;
                }
            }
        }
    }
}
