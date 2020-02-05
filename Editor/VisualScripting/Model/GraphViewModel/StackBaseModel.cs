using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;

namespace UnityEditor.VisualScripting.GraphViewModel
{
    [Serializable]
    public abstract class StackBaseModel : NodeModel, IStackModel
    {
        [SerializeReference]
        List<INodeModel> m_StackedNodeModels = new List<INodeModel>();

#if UNITY_2020_1_OR_NEWER
        public override CapabilityFlags Capabilities => CapabilityFlags.Selectable | CapabilityFlags.Deletable |
        CapabilityFlags.Movable | CapabilityFlags.DeletableWhenEmpty | CapabilityFlags.Copiable;
#else
        public override CapabilityFlags Capabilities => CapabilityFlags.Selectable | CapabilityFlags.Deletable |
        CapabilityFlags.Movable | CapabilityFlags.DeletableWhenEmpty;
#endif

        public virtual IFunctionModel OwningFunctionModel { get; set; }

        public IList<INodeModel> NodeModels => m_StackedNodeModels;

        List<IPortModel> m_OutputPorts = new List<IPortModel>();
        List<IPortModel> m_InputPorts = new List<IPortModel>();

        public virtual bool AcceptNode(Type nodeType)
        {
            // Do not accept more than 1 branched node
            bool isBranchedNode = Attribute.IsDefined(nodeType, typeof(BranchedNodeAttribute));
            foreach (var child in m_StackedNodeModels)
            {
                if (isBranchedNode && Attribute.IsDefined(child.GetType(), typeof(BranchedNodeAttribute)))
                {
                    return false;
                }
            }

            return true;
        }

        public IReadOnlyList<IPortModel> InputPorts => m_InputPorts;
        public override IReadOnlyList<IPortModel> InputsByDisplayOrder => InputPorts;

        public IReadOnlyList<IPortModel> OutputPorts
        {
            get
            {
                return DelegatesOutputsToNode(out var last)
                    ? last.OutputsByDisplayOrder
                        .Where(p => p.PortType == PortType.Execution || p.PortType == PortType.Loop).ToList()
                    : m_OutputPorts;
            }
        }

        public override IReadOnlyList<IPortModel> OutputsByDisplayOrder => OutputPorts;

        public bool DelegatesOutputsToNode(out INodeModel last)
        {
            last = m_StackedNodeModels.LastOrDefault();

            return ModelDelegatesOutputs(last);
        }

        static bool ModelDelegatesOutputs(INodeModel model)
        {
            return model != null && model.IsBranchType && model.OutputsById.Count > 0;
        }

        public void CleanUp()
        {
            m_StackedNodeModels.RemoveAll(n => n == null);
        }

        public TNodeType CreateStackedNode<TNodeType>(string nodeName = "", int index = -1, SpawnFlags spawnFlags = SpawnFlags.Default, Action<TNodeType> setup = null, GUID? guid = null) where TNodeType : NodeModel
        {
            var node = (TNodeType)CreateStackedNode(typeof(TNodeType), nodeName, index, spawnFlags, n => setup?.Invoke((TNodeType)n), guid);
            return node;
        }

        public INodeModel CreateStackedNode(Type nodeTypeToCreate, string nodeName = "", int index = -1, SpawnFlags spawnFlags = SpawnFlags.Default, Action<NodeModel> preDefineSetup = null, GUID? guid = null)
        {
            SpawnFlags createNodeFlags = (spawnFlags & SpawnFlags.CreateNodeAsset) | SpawnFlags.Orphan; // we don't want to CreateNode to add the node to the graph nor to register undo
            var graphModel = (GraphModel)GraphModel;
            NodeModel nodeModel = (NodeModel)graphModel.CreateNodeInternal(nodeTypeToCreate, nodeName, Vector2.zero, createNodeFlags, preDefineSetup, guid);
            graphModel.RegisterNodeGuid(nodeModel);
            if (!spawnFlags.IsOrphan())
            {
                if (spawnFlags.IsUndoable())
                {
                    AddStackedNode(nodeModel, index);
                    AssetModel.SetAssetDirty();
                }
                else
                    AddStackedNode(nodeModel, index);
            }
            nodeModel.DefineNode();

            return nodeModel;
        }

        public void MoveStackedNodes(IReadOnlyCollection<INodeModel> nodesToMove, int actionNewIndex, bool deleteWhenEmpty = true)
        {
            if (nodesToMove == null)
                return;

            int i = 0;
            foreach (var nodeModel in nodesToMove)
            {
                var parentStack = (StackBaseModel)nodeModel.ParentStackModel;
                if (parentStack != null)
                {
                    Undo.RegisterCompleteObjectUndo(SerializableAsset, "Unparent Node");
                    parentStack.RemoveStackedNode(nodeModel);
                    EditorUtility.SetDirty(SerializableAsset);
                    if (deleteWhenEmpty && parentStack.Capabilities.HasFlag(CapabilityFlags.DeletableWhenEmpty) &&
                        parentStack != this &&
                        !parentStack.GetConnectedNodes().Any() &&
                        !parentStack.NodeModels.Any())
                        ((VSGraphModel)GraphModel).DeleteNode(parentStack, GraphViewModel.GraphModel.DeleteConnections.True);
                }
            }

            // We need to do it in two passes to allow for same stack move of multiple nodes.
            foreach (var nodeModel in nodesToMove)
                AddStackedNode(nodeModel, actionNewIndex == -1 ? -1 : actionNewIndex + i++);
        }

        public virtual void AddStackedNode(INodeModel nodeModelInterface, int index)
        {
            if (!AcceptNode(nodeModelInterface.GetType()))
                return;

            var nodeModel = (NodeModel)nodeModelInterface;
            nodeModel.AssetModel = AssetModel;
            nodeModel.ParentStackModel = this;

            if (index == -1)
                m_StackedNodeModels.Add(nodeModel);
            else
                m_StackedNodeModels.Insert(index, nodeModel);

            VSGraphModel vsGraphModel = (VSGraphModel)GraphModel;
            // We need to register before calling TransferConnections(), as edge models rely on the guid to node mapping to resolve ports
            vsGraphModel.UnregisterNodeGuid(nodeModel.Guid);
            vsGraphModel.RegisterNodeGuid(nodeModel);

            bool insertedLast = index == -1 || m_StackedNodeModels.Count == 1 || index == m_StackedNodeModels.Count;
            if (insertedLast && ModelDelegatesOutputs(nodeModelInterface))
                TransferConnections(GraphModel, m_OutputPorts, OutputPorts);


            vsGraphModel.LastChanges.ChangedElements.Add(nodeModel);

            // Needed to add/remove/update the return value port of the node according to the function type
            nodeModel.DefineNode();
        }

        protected static void TransferConnections(IGraphModel graphModel, IReadOnlyList<IPortModel> oldOutputPorts, IReadOnlyList<IPortModel> newOutputPorts)
        {
            var edgesToDelete = new List<IEdgeModel>(oldOutputPorts.Count);
            for (var i = 0; i < oldOutputPorts.Count; i++)
            {
                var oldPort = oldOutputPorts[i];
                var newPort = newOutputPorts.ElementAtOrDefault(i);
                var connections = graphModel.GetEdgesConnections(oldPort).ToList();
                foreach (IEdgeModel edge in connections)
                {
                    edgesToDelete.Add(edge);
                    if (newPort != null && !newPort.Connected)
                    {
                        ((GraphModel)graphModel).CreateEdge(edge.InputPortModel, newPort);
                    }
                }
            }

            ((GraphModel)graphModel).DeleteEdges(edgesToDelete);
        }

        public virtual void RemoveStackedNode(INodeModel nodeModel, EdgeBehaviourOnRemove edgeBehaviour = EdgeBehaviourOnRemove.Ignore)
        {
            ((NodeModel)nodeModel).ParentStackModel = null;
            int index = m_StackedNodeModels.IndexOf(nodeModel);
            if (edgeBehaviour == EdgeBehaviourOnRemove.Transfer && index == m_StackedNodeModels.Count - 1 && ModelDelegatesOutputs(nodeModel))
                TransferConnections(GraphModel, OutputPorts, m_OutputPorts);
            m_StackedNodeModels.RemoveAt(index);

            VSGraphModel vsGraphModel = (VSGraphModel)GraphModel;
            vsGraphModel.UnregisterNodeGuid(nodeModel.Guid);
            vsGraphModel.LastChanges.DeletedElements++;
            vsGraphModel.LastChanges.ChangedElements.Add(this);
        }

        public enum EdgeBehaviourOnRemove
        {
            Ignore,
            Transfer
        }

        protected override void OnPreDefineNode()
        {
            m_InputPorts = new List<IPortModel>();
            m_OutputPorts = new List<IPortModel>();
            base.OnPreDefineNode();
        }

        protected override void OnDefineNode()
        {
            AddInputExecutionPort(null);
            AddExecutionOutputPort(null);
        }

        public override void UndoRedoPerformed()
        {
            // also done in VSGraphModel.OnEnable()
            foreach (var nodeModel in m_StackedNodeModels)
                ((NodeModel)nodeModel).ParentStackModel = this;

            base.UndoRedoPerformed();
        }

        public void ClearNodes()
        {
            m_StackedNodeModels.Clear();
        }

        protected override PortModel AddInputPort(string portName, PortType portType, TypeHandle dataType, string portId = null, PortModel.PortModelOptions options = PortModel.PortModelOptions.Default, Action<ConstantNodeModel> preDefine = null, object portCreationData = null)
        {
            var inputPort = base.AddInputPort(portName, portType, dataType, portId, options, preDefine, portCreationData);
            m_InputPorts.Add(inputPort);
            return inputPort;
        }

        protected override PortModel AddOutputPort(string portName, PortType portType, TypeHandle dataType, string portId = null, PortModel.PortModelOptions options = PortModel.PortModelOptions.Default, object portCreationData = null)
        {
            var outputPort = base.AddOutputPort(portName, portType, dataType, portId, options, portCreationData);
            m_OutputPorts.Add(outputPort);
            return outputPort;
        }
    }
}
