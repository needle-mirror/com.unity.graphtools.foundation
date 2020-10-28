using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class CreateNodeFromSearcherAction : BaseAction
    {
        public Vector2 Position;
        public GraphNodeModelSearcherItem SelectedItem;
        public IReadOnlyList<GUID> Guids;

        public CreateNodeFromSearcherAction()
        {
            UndoString = "Create Node";
        }

        public CreateNodeFromSearcherAction(Vector2 position,
                                            GraphNodeModelSearcherItem selectedItem,
                                            IReadOnlyList<GUID> guids) : this()
        {
            Position = position;
            SelectedItem = selectedItem;
            Guids = guids;
        }

        public static void DefaultReducer(State previousState, CreateNodeFromSearcherAction action)
        {
            previousState.PushUndo(action);

            var nodes = action.SelectedItem.CreateElements.Invoke(
                new GraphNodeCreationData(previousState.CurrentGraphModel, action.Position, guids: action.Guids));

            if (nodes.Any(n => n is IEdgeModel))
                previousState.CurrentGraphModel.LastChanges.ElementsToAutoAlign.AddRange(nodes);

            previousState.MarkForUpdate(UpdateFlags.GraphTopology);
        }
    }

    public class CreateNodeFromPortAction : BaseAction
    {
        public IPortModel PortModel;
        public Vector2 Position;
        public GraphNodeModelSearcherItem SelectedItem;
        public IReadOnlyList<IEdgeModel> EdgesToDelete;
        public bool ItemizeSourceNode;

        public CreateNodeFromPortAction()
        {
            UndoString = "Create Node";
        }

        public CreateNodeFromPortAction(IPortModel portModel, Vector2 position, GraphNodeModelSearcherItem selectedItem,
                                        IReadOnlyList<IEdgeModel> edgesToDelete = null, bool itemizeSourceNode = true) : this()
        {
            PortModel = portModel;
            Position = position;
            SelectedItem = selectedItem;
            EdgesToDelete = edgesToDelete;
            ItemizeSourceNode = itemizeSourceNode;
        }

        public static void DefaultReducer(State previousState, CreateNodeFromPortAction action)
        {
            previousState.PushUndo(action);

            var graphModel = previousState.CurrentGraphModel;
            if (action.EdgesToDelete != null)
                graphModel.DeleteEdges(action.EdgesToDelete);

            var position = action.Position - Vector2.up * EdgeActionConfig.k_NodeOffset;
            var elementModels = action.SelectedItem.CreateElements.Invoke(
                new GraphNodeCreationData(graphModel, position));

            if (!elementModels.Any() || !(elementModels[0] is IPortNode selectedNodeModel))
                return;

            var otherPortModel = selectedNodeModel.GetPortFitToConnectTo(action.PortModel);

            if (otherPortModel == null)
                return;

            var thisPortModel = action.PortModel;

            IEdgeModel newEdge = null;
            if (thisPortModel.Direction == Direction.Output)
            {
                if (action.ItemizeSourceNode)
                    graphModel.CreateItemizedNode(previousState, EdgeActionConfig.k_NodeOffset, ref thisPortModel);

                newEdge = graphModel.CreateEdge(otherPortModel, thisPortModel);
            }
            else
            {
                newEdge = graphModel.CreateEdge(thisPortModel, otherPortModel);
            }

            if (newEdge != null && previousState.Preferences.GetBool(BoolPref.AutoAlignDraggedEdges))
                graphModel.LastChanges?.ElementsToAutoAlign.Add(newEdge);

            graphModel.LastChanges?.ChangedElements.Add(action.PortModel.NodeModel);
        }
    }

    public class CreateNodeOnEdgeAction : BaseAction
    {
        public IEdgeModel EdgeModel;
        public Vector2 Position;
        public GraphNodeModelSearcherItem SelectedItem;
        public GUID? Guid;

        public CreateNodeOnEdgeAction()
        {
            UndoString = "Create Node";
        }

        public CreateNodeOnEdgeAction(IEdgeModel edgeModel, Vector2 position,
                                      GraphNodeModelSearcherItem selectedItem, GUID? guid = null) : this()
        {
            EdgeModel = edgeModel;
            Position = position;
            SelectedItem = selectedItem;
            Guid = guid;
        }

        public static void DefaultReducer(State previousState, CreateNodeOnEdgeAction action)
        {
            previousState.PushUndo(action);

            var edgeInput = action.EdgeModel.ToPort;
            var edgeOutput = action.EdgeModel.FromPort;

            // Instantiate node
            var graphModel = previousState.CurrentGraphModel;

            var position = action.Position - Vector2.up * EdgeActionConfig.k_NodeOffset;

            List<GUID> guids = action.Guid.HasValue ? new List<GUID> { action.Guid.Value } : null;

            var elementModels = action.SelectedItem.CreateElements.Invoke(
                new GraphNodeCreationData(graphModel, position, guids: guids));

            if (elementModels.Length == 0 || !(elementModels[0] is IInOutPortsNode selectedNodeModel))
                return;

            // Delete old edge
            graphModel.DeleteEdge(action.EdgeModel);

            // Connect input port
            var inputPortModel = selectedNodeModel.InputsByDisplayOrder.FirstOrDefault(p => p?.PortType == edgeOutput?.PortType);

            if (inputPortModel != null)
                graphModel.CreateEdge(inputPortModel, edgeOutput);

            // Find first matching output type and connect it
            var outputPortModel = selectedNodeModel.GetPortFitToConnectTo(edgeInput);

            if (outputPortModel != null)
                graphModel.CreateEdge(edgeInput, outputPortModel);
        }
    }
}
