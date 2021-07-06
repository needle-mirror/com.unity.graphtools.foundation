using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// A model that represents a graph.
    /// </summary>
    [Serializable]
    [MovedFrom(false, sourceAssembly: "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public abstract class GraphModel : IGraphModel, ISerializationCallbackReceiver
    {
        [SerializeReference]
        GraphAssetModel m_AssetModel;

        [SerializeReference]
        List<INodeModel> m_GraphNodeModels;

        [SerializeField, Obsolete]
        List<EdgeModel> m_EdgeModels;

        [SerializeReference]
        List<IBadgeModel> m_BadgeModels;

        [SerializeReference]
        List<IEdgeModel> m_GraphEdgeModels;

        [SerializeReference, Obsolete]
        List<EdgeModel> m_PolymorphicEdgeModels;

        [SerializeField, Obsolete]
        List<StickyNoteModel> m_StickyNoteModels;

        [SerializeReference]
        List<IStickyNoteModel> m_GraphStickyNoteModels;

        [SerializeField, Obsolete]
        List<PlacematModel> m_PlacematModels;

        [SerializeReference]
        List<IPlacematModel> m_GraphPlacematModels;

        [SerializeReference]
        List<IVariableDeclarationModel> m_GraphVariableModels;

        [SerializeReference]
        List<IDeclarationModel> m_GraphPortalModels;

        [SerializeField, FormerlySerializedAs("name")]
        string m_Name;

        [SerializeField]
        string m_StencilTypeName; // serialized as string, resolved as type by ISerializationCallbackReceiver

        Type m_StencilType;

        // As this field is not serialized, use GetElementsByGuid() to access it.
        Dictionary<SerializableGUID, IGraphElementModel> m_ElementsByGuid;

        /// <inheritdoc />
        public virtual Type DefaultStencilType => null;

        /// <inheritdoc />
        public Type StencilType
        {
            get => m_StencilType;
            set
            {
                if (value == null)
                    value = DefaultStencilType;
                Assert.IsTrue(typeof(IStencil).IsAssignableFrom(value));
                m_StencilType = value;
                Stencil = InstantiateStencil(StencilType);
            }
        }

        /// <inheritdoc />
        public IStencil Stencil { get; private set; }

        /// <inheritdoc />
        public IGraphAssetModel AssetModel
        {
            get => m_AssetModel;
            set => m_AssetModel = (GraphAssetModel)value;
        }

        /// <inheritdoc />
        public IReadOnlyList<INodeModel> NodeModels => m_GraphNodeModels;

        /// <inheritdoc />
        public IReadOnlyList<IEdgeModel> EdgeModels => m_GraphEdgeModels;

        /// <inheritdoc />
        public IReadOnlyList<IBadgeModel> BadgeModels => m_BadgeModels;

        /// <inheritdoc />
        public IReadOnlyList<IStickyNoteModel> StickyNoteModels => m_GraphStickyNoteModels;

        /// <inheritdoc />
        public IReadOnlyList<IPlacematModel> PlacematModels => m_GraphPlacematModels;

        /// <inheritdoc />
        public IReadOnlyList<IVariableDeclarationModel> VariableDeclarations => m_GraphVariableModels;

        /// <inheritdoc />
        public IReadOnlyList<IDeclarationModel> PortalDeclarations => m_GraphPortalModels;

        /// <inheritdoc />
        public string Name => m_AssetModel ? m_AssetModel.Name : "";

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphModel"/> class.
        /// </summary>
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

        /// <summary>
        /// Determines whether two ports can be connected together by an edge.
        /// </summary>
        /// <param name="startPortModel">The port from which the edge would come from.</param>
        /// <param name="compatiblePortModel">The port to which the edge would got to.</param>
        /// <returns>True if the two ports can be connected. False otherwise.</returns>
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

        /// <inheritdoc />
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

        Dictionary<SerializableGUID, IGraphElementModel> GetElementsByGuid()
        {
            if (m_ElementsByGuid == null)
                BuildElementByGuidDictionary();

            return m_ElementsByGuid;
        }

        /// <inheritdoc />
        public bool TryGetModelFromGuid(SerializableGUID guid, out IGraphElementModel model)
        {
            return GetElementsByGuid().TryGetValue(guid, out model);
        }

        /// <summary>
        /// Adds a node model to the graph.
        /// </summary>
        /// <param name="nodeModel">The node model to add.</param>
        public void AddNode(INodeModel nodeModel)
        {
            GetElementsByGuid().Add(nodeModel.Guid, nodeModel);

            nodeModel.AssetModel = AssetModel;
            m_GraphNodeModels.Add(nodeModel);
        }

        /// <summary>
        /// Replaces node model at index.
        /// </summary>
        /// <param name="index">Index of the node model in the NodeModels list.</param>
        /// <param name="nodeModel">The new node model.</param>
        protected void ReplaceNode(int index, INodeModel nodeModel)
        {
            GetElementsByGuid().Remove(m_GraphNodeModels[index].Guid);
            GetElementsByGuid().Add(nodeModel.Guid, nodeModel);

            m_GraphNodeModels[index] = nodeModel;
        }

        /// <summary>
        /// Removes a node model from the graph.
        /// </summary>
        /// <param name="nodeModel"></param>
        protected void RemoveNode(INodeModel nodeModel)
        {
            GetElementsByGuid().Remove(nodeModel.Guid);
            m_GraphNodeModels.Remove(nodeModel);
        }

        /// <summary>
        /// Adds a portal declaration model to the graph.
        /// </summary>
        /// <param name="declarationModel">The portal declaration to add.</param>
        protected void AddPortal(IDeclarationModel declarationModel)
        {
            GetElementsByGuid().Add(declarationModel.Guid, declarationModel);
            m_GraphPortalModels.Add(declarationModel);
        }

        /// <summary>
        /// Removes a portal declaration model from the graph.
        /// </summary>
        /// <param name="declarationModel">The portal declaration to remove.</param>
        protected void RemovePortal(IDeclarationModel declarationModel)
        {
            GetElementsByGuid().Remove(declarationModel.Guid);
            m_GraphPortalModels.Remove(declarationModel);
        }

        /// <summary>
        /// Adds an edge to the graph.
        /// </summary>
        /// <param name="edgeModel">The edge to add.</param>
        protected void AddEdge(IEdgeModel edgeModel)
        {
            GetElementsByGuid().Add(edgeModel.Guid, edgeModel);
            m_GraphEdgeModels.Add(edgeModel);
        }

        /// <summary>
        /// Removes an edge from th graph.
        /// </summary>
        /// <param name="edgeModel">The edge to remove.</param>
        protected void RemoveEdge(IEdgeModel edgeModel)
        {
            GetElementsByGuid().Remove(edgeModel.Guid);
            m_GraphEdgeModels.Remove(edgeModel);
        }

        /// <inheritdoc />
        public void AddBadge(IBadgeModel badgeModel)
        {
            GetElementsByGuid().Add(badgeModel.Guid, badgeModel);
            m_BadgeModels.Add(badgeModel);
        }

        /// <summary>
        /// Removes a badge from the graph.
        /// </summary>
        /// <param name="badgeModel">The badge to remove.</param>
        public void RemoveBadge(IBadgeModel badgeModel)
        {
            GetElementsByGuid().Remove(badgeModel.Guid);
            m_BadgeModels.Remove(badgeModel);
        }

        /// <summary>
        /// Adds a sticky note to the graph.
        /// </summary>
        /// <param name="stickyNoteModel">The sticky note to add.</param>
        protected void AddStickyNote(IStickyNoteModel stickyNoteModel)
        {
            GetElementsByGuid().Add(stickyNoteModel.Guid, stickyNoteModel);
            m_GraphStickyNoteModels.Add(stickyNoteModel);
        }

        /// <summary>
        /// Removes a sticky note from the graph.
        /// </summary>
        /// <param name="stickyNoteModel">The sticky note to remove.</param>
        protected void RemoveStickyNote(IStickyNoteModel stickyNoteModel)
        {
            GetElementsByGuid().Remove(stickyNoteModel.Guid);
            m_GraphStickyNoteModels.Remove(stickyNoteModel);
        }

        /// <summary>
        /// Adds a placemat to the graph.
        /// </summary>
        /// <param name="placematModel">The placemat to add.</param>
        protected void AddPlacemat(IPlacematModel placematModel)
        {
            GetElementsByGuid().Add(placematModel.Guid, placematModel);
            m_GraphPlacematModels.Add(placematModel);
        }

        /// <summary>
        /// Removes a placemat from the graph.
        /// </summary>
        /// <param name="placematModel">The placemat to remove.</param>
        protected void RemovePlacemat(IPlacematModel placematModel)
        {
            GetElementsByGuid().Remove(placematModel.Guid);
            m_GraphPlacematModels.Remove(placematModel);
        }

        /// <summary>
        /// Adds a variable declaration to the graph.
        /// </summary>
        /// <param name="variableDeclarationModel">The variable declaration to add.</param>
        protected void AddVariableDeclaration(IVariableDeclarationModel variableDeclarationModel)
        {
            GetElementsByGuid().Add(variableDeclarationModel.Guid, variableDeclarationModel);
            m_GraphVariableModels.Add(variableDeclarationModel);
        }

        /// <summary>
        /// Removes a variable declaration from the graph.
        /// </summary>
        /// <param name="variableDeclarationModel">The variable declaration to remove.</param>
        protected void RemoveVariableDeclaration(IVariableDeclarationModel variableDeclarationModel)
        {
            GetElementsByGuid().Remove(variableDeclarationModel.Guid);
            m_GraphVariableModels.Remove(variableDeclarationModel);
        }

        /// <summary>
        /// Rebuilds the dictionary mapping guids to graph element models.
        /// </summary>
        /// <remarks>
        /// Override this function if your graph models holds new graph elements types.
        /// Ensure that all additional graph element model are added to the guid to model mapping.
        /// </remarks>
        protected virtual void BuildElementByGuidDictionary()
        {
            m_ElementsByGuid = new Dictionary<SerializableGUID, IGraphElementModel>();

            foreach (var model in m_GraphNodeModels)
            {
                AddGuidToModelMapping(model);
            }

            foreach (var model in m_BadgeModels)
            {
                AddGuidToModelMapping(model);
            }

            foreach (var model in m_GraphEdgeModels)
            {
                AddGuidToModelMapping(model);
            }

            foreach (var model in m_GraphStickyNoteModels)
            {
                AddGuidToModelMapping(model);
            }

            foreach (var model in m_GraphPlacematModels)
            {
                AddGuidToModelMapping(model);
            }

            foreach (var model in m_GraphVariableModels)
            {
                AddGuidToModelMapping(model);
            }

            foreach (var model in m_GraphPortalModels)
            {
                AddGuidToModelMapping(model);
            }
        }

        /// <summary>
        /// Adds the model to the dictionary of elements by GUIDs.
        /// </summary>
        /// <remarks>
        /// Helper function to add an entry to the m_ElementsByGuid dictionary.
        /// <para>Use this function when overriding BuildElementByGuidDictionary(),
        /// to add your own entries in the dictionary.</para>
        /// </remarks>
        /// <param name="model">The model to add. Its Guid must already be set.</param>
        protected void AddGuidToModelMapping(IGraphElementModel model)
        {
            m_ElementsByGuid.Add(model.Guid, model);
        }

        /// <summary>
        /// Instantiates an object of type <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type of the object to instantiate.</param>
        /// <typeparam name="InterfaceT">A base type for <paramref name="type"/>.</typeparam>
        /// <returns>A new object.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="type"/> does not derive from <typeparamref name="InterfaceT"/></exception>
        protected InterfaceT Instantiate<InterfaceT>(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            InterfaceT obj;
            if (typeof(InterfaceT).IsAssignableFrom(type))
                obj = (InterfaceT)Activator.CreateInstance(type);
            else
                throw new ArgumentOutOfRangeException(nameof(type));

            return obj;
        }

        /// <inheritdoc />
        public INodeModel CreateNode(Type nodeTypeToCreate, string nodeName, Vector2 position,
            SerializableGUID guid = default, Action<INodeModel> initializationCallback = null, SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            var nodeModel = InstantiateNode(nodeTypeToCreate, nodeName, position, guid, initializationCallback);

            if (!spawnFlags.IsOrphan())
            {
                AddNode(nodeModel);
            }

            return nodeModel;
        }

        /// <summary>
        /// Instantiates a new node.
        /// </summary>
        /// <param name="nodeTypeToCreate">The type of the new node to create.</param>
        /// <param name="nodeName">The name of the node to create.</param>
        /// <param name="position">The position of the node to create.</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item.</param>
        /// <param name="initializationCallback">An initialization method to be called right after the node is created.</param>
        /// <returns>The newly created node.</returns>
        protected virtual INodeModel InstantiateNode(Type nodeTypeToCreate, string nodeName, Vector2 position,
            SerializableGUID guid = default, Action<INodeModel> initializationCallback = null)
        {
            if (nodeTypeToCreate == null)
                throw new ArgumentNullException(nameof(nodeTypeToCreate));

            INodeModel nodeModel;
            if (typeof(IConstant).IsAssignableFrom(nodeTypeToCreate))
                nodeModel = new ConstantNodeModel { Value = (IConstant)Activator.CreateInstance(nodeTypeToCreate) };
            else if (typeof(INodeModel).IsAssignableFrom(nodeTypeToCreate))
                nodeModel = (INodeModel)Activator.CreateInstance(nodeTypeToCreate);
            else
                throw new ArgumentOutOfRangeException(nameof(nodeTypeToCreate));

            if (nodeModel is IHasTitle titled)
                titled.Title = nodeName ?? nodeTypeToCreate.Name;

            nodeModel.Position = position;
            nodeModel.Guid = guid.Valid ? guid : SerializableGUID.Generate();
            nodeModel.AssetModel = AssetModel;
            initializationCallback?.Invoke(nodeModel);
            nodeModel.OnCreateNode();

            return nodeModel;
        }

        /// <inheritdoc />
        public virtual IVariableNodeModel CreateVariableNode(IVariableDeclarationModel declarationModel,
            Vector2 position, SerializableGUID guid = default, SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            return this.CreateNode<VariableNodeModel>(declarationModel.DisplayTitle, position, guid, v => v.DeclarationModel = declarationModel, spawnFlags);
        }

        /// <inheritdoc />
        public virtual IConstantNodeModel CreateConstantNode(TypeHandle constantTypeHandle, string constantName,
            Vector2 position, SerializableGUID guid = default, Action<IConstantNodeModel> initializationCallback = null, SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            var nodeType = Stencil.GetConstantNodeValueType(constantTypeHandle);

            void PreDefineSetup(INodeModel model)
            {
                if (model is IConstantNodeModel constantModel)
                {
                    constantModel.PredefineSetup();
                    initializationCallback?.Invoke(constantModel);
                }
            }

            return (IConstantNodeModel)CreateNode(nodeType, constantName, position, guid, PreDefineSetup, spawnFlags);
        }

        /// <inheritdoc />
        public IReadOnlyCollection<IGraphElementModel> DeleteBadges()
        {
            var deletedBadges = new List<IGraphElementModel>(m_BadgeModels);

            foreach (var model in deletedBadges)
            {
                m_ElementsByGuid.Remove(model.Guid);
            }
            m_BadgeModels.Clear();

            return deletedBadges;
        }

        /// <inheritdoc />
        public IReadOnlyCollection<IGraphElementModel> DeleteBadgesOfType<T>() where T : IBadgeModel
        {
            var deletedBadges = m_BadgeModels
                .Where(b => b is T)
                .ToList();

            foreach (var model in deletedBadges)
            {
                m_ElementsByGuid.Remove(model.Guid);
            }

            m_BadgeModels = m_BadgeModels
                .Where(b => !(b is T))
                .ToList();

            return deletedBadges;
        }

        /// <inheritdoc />
        public INodeModel DuplicateNode(INodeModel sourceNode, Vector2 delta)
        {
            var pastedNodeModel = sourceNode.Clone();

            // Set graphmodel BEFORE define node as it is commonly use during Define
            pastedNodeModel.AssetModel = AssetModel;
            pastedNodeModel.AssignNewGuid();
            pastedNodeModel.OnDuplicateNode(sourceNode);

            AddNode(pastedNodeModel);
            pastedNodeModel.Position += delta;

            return pastedNodeModel;
        }

        /// <inheritdoc />
        public virtual IEdgeModel DuplicateEdge(IEdgeModel sourceEdge, INodeModel targetInputNode, INodeModel targetOutputNode)
        {
            IPortModel inputPortModel = null;
            IPortModel outputPortModel = null;
            if (targetInputNode != null && targetOutputNode != null)
            {
                // Both node were duplicated; create a new edge between the duplicated nodes.
                inputPortModel = (targetInputNode as IInputOutputPortsNodeModel)?.InputsById[sourceEdge.ToPortId];
                outputPortModel = (targetOutputNode as IInputOutputPortsNodeModel)?.OutputsById[sourceEdge.FromPortId];
            }
            else if (targetInputNode != null)
            {
                inputPortModel = (targetInputNode as IInputOutputPortsNodeModel)?.InputsById[sourceEdge.ToPortId];
                outputPortModel = sourceEdge.FromPort;
            }
            else if (targetOutputNode != null)
            {
                inputPortModel = sourceEdge.ToPort;
                outputPortModel = (targetOutputNode as IInputOutputPortsNodeModel)?.OutputsById[sourceEdge.FromPortId];
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

        /// <inheritdoc />
        public IInputOutputPortsNodeModel CreateItemizedNode(int nodeOffset, ref IPortModel outputPortModel)
        {
            if (outputPortModel.IsConnected())
            {
                Vector2 offset = Vector2.up * nodeOffset;
                var nodeToConnect = DuplicateNode(outputPortModel.NodeModel, offset) as IInputOutputPortsNodeModel;
                outputPortModel = nodeToConnect?.OutputsById[outputPortModel.UniqueName];
                return nodeToConnect;
            }
            return null;
        }

        /// <inheritdoc />
        public IReadOnlyCollection<IGraphElementModel> DeleteNodes(IReadOnlyCollection<INodeModel> nodeModels, bool deleteConnections)
        {
            var deletedModels = new List<IGraphElementModel>();

            foreach (var nodeModel in nodeModels.Where(n => n.IsDeletable()))
            {
                RemoveNode(nodeModel);
                deletedModels.Add(nodeModel);

                if (deleteConnections)
                {
                    var connectedEdges = nodeModel.GetConnectedEdges().ToList();
                    deletedModels.AddRange(DeleteEdges(connectedEdges));
                }

                // If this is the last portal with the given declaration, delete the declaration.
                if (nodeModel is EdgePortalModel edgePortalModel &&
                    !this.FindReferencesInGraph<IEdgePortalModel>(edgePortalModel.DeclarationModel).Any() &&
                    edgePortalModel.DeclarationModel != null)
                {
                    RemovePortal(edgePortalModel.DeclarationModel);
                    deletedModels.Add(edgePortalModel.DeclarationModel);
                }

                nodeModel.Destroy();
            }

            return deletedModels;
        }

        /// <summary>
        /// Returns the type of edge to instantiate between two ports.
        /// </summary>
        /// <param name="toPort">The destination port.</param>
        /// <param name="fromPort">The origin port.</param>
        /// <returns>The edge model type.</returns>
        protected virtual Type GetEdgeType(IPortModel toPort, IPortModel fromPort)
        {
            return typeof(EdgeModel);
        }

        /// <inheritdoc />
        public IEdgeModel CreateEdge(IPortModel toPort, IPortModel fromPort, SerializableGUID guid = default)
        {
            var existing = this.GetEdgeConnectedToPorts(toPort, fromPort);
            if (existing != null)
                return existing;

            var edgeModel = InstantiateEdge(toPort, fromPort, guid);
            AddEdge(edgeModel);
            return edgeModel;
        }

        /// <summary>
        /// Instantiates an edge.
        /// </summary>
        /// <param name="toPort">The port from which the edge originates.</param>
        /// <param name="fromPort">The port to which the edge goes.</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item.</param>
        /// <returns>The newly created edge</returns>
        protected virtual IEdgeModel InstantiateEdge(IPortModel toPort, IPortModel fromPort, SerializableGUID guid = default)
        {
            var edgeType = GetEdgeType(toPort, fromPort);
            var edgeModel = Instantiate<IEdgeModel>(edgeType);
            edgeModel.AssetModel = AssetModel;
            edgeModel.Guid = guid.Valid ? guid : SerializableGUID.Generate();
            edgeModel.EdgeLabel = "";
            edgeModel.SetPorts(toPort, fromPort);
            return edgeModel;
        }

        /// <inheritdoc />
        public IReadOnlyCollection<IGraphElementModel> DeleteEdges(IReadOnlyCollection<IEdgeModel> edgeModels)
        {
            var deletedModels = new List<IGraphElementModel>();

            foreach (var edgeModel in edgeModels.Where(e => e != null && e.IsDeletable()))
            {
                edgeModel.ToPort?.NodeModel?.OnDisconnection(edgeModel.ToPort, edgeModel.FromPort);
                edgeModel.FromPort?.NodeModel?.OnDisconnection(edgeModel.FromPort, edgeModel.ToPort);

                RemoveEdge(edgeModel);
                deletedModels.Add(edgeModel);
            }

            return deletedModels;
        }

        /// <summary>
        /// Returns the type of sticky note to instantiate.
        /// </summary>
        /// <returns>The sticky note model type.</returns>
        protected virtual Type GetStickyNoteType()
        {
            return typeof(StickyNoteModel);
        }

        /// <inheritdoc />
        public IStickyNoteModel CreateStickyNote(Rect position, SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            var stickyNoteModel = InstantiateStickyNote(position);
            if (!spawnFlags.IsOrphan())
            {
                AddStickyNote(stickyNoteModel);
            }
            return stickyNoteModel;
        }

        /// <summary>
        /// Instantiates a new sticky note.
        /// </summary>
        /// <param name="position">The position of the sticky note to create.</param>
        /// <returns>The newly created sticky note</returns>
        protected virtual IStickyNoteModel InstantiateStickyNote(Rect position)
        {
            var stickyNoteModelType = GetStickyNoteType();
            var stickyNoteModel = Instantiate<IStickyNoteModel>(stickyNoteModelType);
            stickyNoteModel.PositionAndSize = position;
            stickyNoteModel.AssetModel = AssetModel;
            return stickyNoteModel;
        }

        /// <inheritdoc />
        public IReadOnlyCollection<IGraphElementModel> DeleteStickyNotes(IReadOnlyCollection<IStickyNoteModel> stickyNoteModels)
        {
            var deletedModels = new List<IGraphElementModel>();

            foreach (var stickyNoteModel in stickyNoteModels.Where(s => s.IsDeletable()))
            {
                RemoveStickyNote(stickyNoteModel);
                stickyNoteModel.Destroy();
                deletedModels.Add(stickyNoteModel);
            }

            return deletedModels;
        }

        /// <summary>
        /// Returns the type of placemat to instantiate.
        /// </summary>
        /// <returns>The placemat model type.</returns>
        protected virtual Type GetPlacematType()
        {
            return typeof(PlacematModel);
        }

        /// <inheritdoc />
        public IPlacematModel CreatePlacemat(Rect position, SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            var placematModel = InstantiatePlacemat(position);
            if (!spawnFlags.IsOrphan())
            {
                placematModel.ZOrder = this.GetPlacematMaxZOrder() + 1;

                AddPlacemat(placematModel);
            }

            return placematModel;
        }

        /// <summary>
        /// Instantiates a new placemat.
        /// </summary>
        /// <param name="position">The position of the placemat to create.</param>
        /// <returns>The newly created placemat</returns>
        protected virtual IPlacematModel InstantiatePlacemat(Rect position)
        {
            var placematModelType = GetPlacematType();
            var placematModel = Instantiate<IPlacematModel>(placematModelType);
            placematModel.PositionAndSize = position;
            placematModel.AssetModel = AssetModel;
            placematModel.ZOrder = 0;
            return placematModel;
        }

        /// <inheritdoc />
        public IReadOnlyCollection<IGraphElementModel> DeletePlacemats(IReadOnlyCollection<IPlacematModel> placematModels)
        {
            var deletedModels = new List<IGraphElementModel>();

            foreach (var placematModel in placematModels.Where(p => p.IsDeletable()))
            {
                RemovePlacemat(placematModel);
                placematModel.Destroy();
                deletedModels.Add(placematModel);
            }

            return deletedModels;
        }

        IStencil InstantiateStencil(Type stencilType)
        {
            Debug.Assert(typeof(IStencil).IsAssignableFrom(stencilType));
            var stencil = (IStencil)Activator.CreateInstance(stencilType);
            Assert.IsNotNull(stencil);
            stencil.GraphModel = this;
            return stencil;
        }

        /// <summary>
        /// Returns the type of variable declaration to instantiate.
        /// </summary>
        /// <returns>The variable declaration model type.</returns>
        protected virtual Type GetDefaultVariableDeclarationType()
        {
            return typeof(VariableDeclarationModel);
        }

        /// <inheritdoc />
        public IVariableDeclarationModel CreateGraphVariableDeclaration(TypeHandle variableDataType, string variableName,
            ModifierFlags modifierFlags, bool isExposed, IConstant initializationModel = null, SerializableGUID guid = default,
            SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            return CreateGraphVariableDeclaration(GetDefaultVariableDeclarationType(), variableDataType, variableName,
                modifierFlags, isExposed, initializationModel, guid, InitCallback, spawnFlags);

            void InitCallback(IVariableDeclarationModel variableDeclaration, IConstant initModel)
            {
                if (variableDeclaration is VariableDeclarationModel basicVariableDeclarationModel)
                {
                    basicVariableDeclarationModel.variableFlags = VariableFlags.None;

                    if (initModel != null) basicVariableDeclarationModel.InitializationModel = initModel;
                }
            }
        }

        /// <inheritdoc />
        public IVariableDeclarationModel CreateGraphVariableDeclaration(Type variableTypeToCreate,
            TypeHandle variableDataType, string variableName, ModifierFlags modifierFlags, bool isExposed,
            IConstant initializationModel = null, SerializableGUID guid = default, Action<IVariableDeclarationModel, IConstant> initializationCallback = null,
            SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            var variableDeclaration = InstantiateVariableDeclaration(variableTypeToCreate, variableDataType,
                variableName, modifierFlags, isExposed, initializationModel, guid, initializationCallback);

            if (initializationModel == null && !spawnFlags.IsOrphan())
                variableDeclaration.CreateInitializationValue();

            if (!spawnFlags.IsOrphan())
                AddVariableDeclaration(variableDeclaration);

            return variableDeclaration;
        }

        /// <summary>
        /// Instantiates a new variable declaration.
        /// </summary>
        /// <param name="variableTypeToCreate">The type of variable to create.</param>
        /// <param name="variableDataType">The type of data the new variable declaration to create represents.</param>
        /// <param name="variableName">The name of the new variable declaration to create.</param>
        /// <param name="modifierFlags">The modifier flags of the new variable declaration to create.</param>
        /// <param name="isExposed">Whether the variable is exposed externally or not.</param>
        /// <param name="initializationModel">The initialization model of the new variable declaration to create. Can be <code>null</code>.</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item. If none is provided, a new
        /// SerializableGUID will be generated for it.</param>
        /// <param name="initializationCallback">An initialization method to be called right after the variable declaration is created.</param>
        /// <returns>The newly created variable declaration.</returns>
        protected virtual IVariableDeclarationModel InstantiateVariableDeclaration(Type variableTypeToCreate,
            TypeHandle variableDataType, string variableName, ModifierFlags modifierFlags, bool isExposed,
            IConstant initializationModel, SerializableGUID guid, Action<IVariableDeclarationModel, IConstant> initializationCallback = null)
        {
            var variableDeclaration = Instantiate<IVariableDeclarationModel>(variableTypeToCreate);

            variableDeclaration.Guid = guid.Valid ? guid : SerializableGUID.Generate();
            variableDeclaration.AssetModel = AssetModel;
            variableDeclaration.DataType = variableDataType;
            variableDeclaration.Title = variableName;
            variableDeclaration.IsExposed = isExposed;
            variableDeclaration.Modifiers = modifierFlags;

            initializationCallback?.Invoke(variableDeclaration, initializationModel);

            return variableDeclaration;
        }

        /// <inheritdoc />
        public virtual TDeclType DuplicateGraphVariableDeclaration<TDeclType>(TDeclType sourceModel)
            where TDeclType : IVariableDeclarationModel
        {
            var uniqueName = sourceModel.Title;
            var copy = sourceModel.Clone();
            copy.Title = uniqueName;
            if (copy.InitializationModel != null)
            {
                copy.CreateInitializationValue();
                copy.InitializationModel.ObjectValue = sourceModel.InitializationModel.ObjectValue;
            }

            AddVariableDeclaration(copy);

            return copy;
        }

        /// <inheritdoc />
        public IReadOnlyCollection<IGraphElementModel> DeleteVariableDeclarations(IReadOnlyCollection<IVariableDeclarationModel> variableModels, bool deleteUsages = true)
        {
            var deletedModels = new List<IGraphElementModel>();

            foreach (var variableModel in variableModels.Where(v => v.IsDeletable()))
            {
                RemoveVariableDeclaration(variableModel);
                deletedModels.Add(variableModel);

                if (deleteUsages)
                {
                    var nodesToDelete = this.FindReferencesInGraph(variableModel).Cast<INodeModel>().ToList();
                    deletedModels.AddRange(DeleteNodes(nodesToDelete, deleteConnections: true));
                }
            }

            return deletedModels;
        }

        /// <summary>
        /// Returns the type of portal to instantiate.
        /// </summary>
        /// <returns>The portal model type.</returns>
        protected virtual Type GetPortalType()
        {
            return typeof(DeclarationModel);
        }

        /// <inheritdoc />
        public IDeclarationModel CreateGraphPortalDeclaration(string portalName, SerializableGUID guid = default, SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            var decl = InstantiatePortal(portalName, guid);

            if (!spawnFlags.IsOrphan())
            {
                AddPortal(decl);
            }

            return decl;
        }

        /// <summary>
        /// Instantiates a new portal model.
        /// </summary>
        /// <param name="portalName">The name of the portal</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item.</param>
        /// <returns>The newly created declaration model</returns>
        protected virtual IDeclarationModel InstantiatePortal(string portalName, SerializableGUID guid = default)
        {
            var portalModelType = GetPortalType();
            var portalModel = Instantiate<IDeclarationModel>(portalModelType);
            portalModel.Title = portalName;
            portalModel.Guid = guid.Valid ? guid : SerializableGUID.Generate();
            return portalModel;
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public IEdgePortalEntryModel CreateEntryPortalFromEdge(IEdgeModel edgeModel)
        {
            var outputPortModel = edgeModel.FromPort;
            if (outputPortModel.PortType == PortType.Execution)
                return this.CreateNode<ExecutionEdgePortalEntryModel>();

            return this.CreateNode<DataEdgePortalEntryModel>();
        }

        /// <inheritdoc />
        public IEdgePortalExitModel CreateExitPortalFromEdge(IEdgeModel edgeModel)
        {
            var inputPortModel = edgeModel.ToPort;
            if (inputPortModel?.PortType == PortType.Execution)
                return this.CreateNode<ExecutionEdgePortalExitModel>();

            return this.CreateNode<DataEdgePortalExitModel>();
        }

        /// <inheritdoc />
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

            if (m_GraphNodeModels == null)
                m_GraphNodeModels = new List<INodeModel>();

            foreach (var model in NodeModels)
            {
                if (model is null)
                    continue;
                model.AssetModel = AssetModel;
            }

            foreach (var nodeModel in NodeModels)
            {
                (nodeModel as NodeModel)?.DefineNode();
            }

            MigrateNodes();
        }

        /// <summary>
        /// Callback to migrate nodes from an old asset to the new models.
        /// </summary>
        protected virtual void MigrateNodes()
        {
        }

        /// <inheritdoc />
        public void OnDisable()
        {
        }

        /// <inheritdoc />
        public void Dispose() { }

        /// <inheritdoc />
        public void UndoRedoPerformed()
        {
            OnEnable();
            foreach (var edgeModel in EdgeModels.OfType<EdgeModel>())
            {
                edgeModel.ResetPorts();
            }
        }

        /// <inheritdoc />
        public virtual bool CheckIntegrity(Verbosity errors)
        {
            return GraphModelExtensions.CheckIntegrity(this, errors);
        }

        /// <inheritdoc />
        public virtual void OnBeforeSerialize()
        {
            if (StencilType != null)
                m_StencilTypeName = StencilType.AssemblyQualifiedName;
        }

        /// <inheritdoc />
        public virtual void OnAfterDeserialize()
        {
            if (!string.IsNullOrEmpty(m_StencilTypeName))
                StencilType = Type.GetType(m_StencilTypeName) ?? DefaultStencilType;
        }

        /// <inheritdoc />
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
                elementMapping.Add(sourceEdge.Guid.ToString(), sourceEdge);
            }

            foreach (var sourceVariableNode in sourceGraphModel.NodeModels.Where(model => model is VariableNodeModel))
            {
                elementMapping.TryGetValue(sourceVariableNode.Guid.ToString(), out var newNode);

                if (newNode != null)
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
