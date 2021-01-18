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

        public static void DefaultReducer(State state, CreateNodeFromSearcherAction action)
        {
            state.PushUndo(action);

            var newModels = action.SelectedItem.CreateElements.Invoke(
                new GraphNodeCreationData(state.GraphModel, action.Position, guids: action.Guids));

            state.MarkNew(newModels);
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

        public static void DefaultReducer(State state, CreateNodeFromPortAction action)
        {
            state.PushUndo(action);

            var graphModel = state.GraphModel;
            if (action.EdgesToDelete != null)
            {
                var deletedModels = graphModel.DeleteEdges(action.EdgesToDelete);
                state.MarkDeleted(deletedModels);
            }

            var position = action.Position - Vector2.up * EdgeActionConfig.nodeOffset;
            var elementModels = action.SelectedItem.CreateElements.Invoke(
                new GraphNodeCreationData(graphModel, position));

            state.MarkNew(elementModels);

            if (!elementModels.Any() || !(elementModels[0] is IPortNode selectedNodeModel))
                return;

            var otherPortModel = selectedNodeModel.GetPortFitToConnectTo(action.PortModel);

            if (otherPortModel == null)
                return;

            var thisPortModel = action.PortModel;

            IEdgeModel newEdge;
            if (thisPortModel.Direction == Direction.Output)
            {
                if (action.ItemizeSourceNode)
                {
                    graphModel.CreateItemizedNode(state, EdgeActionConfig.nodeOffset, ref thisPortModel);
                    state.RequestUIRebuild();
                }

                newEdge = graphModel.CreateEdge(otherPortModel, thisPortModel);
                state.MarkNew(newEdge);
            }
            else
            {
                newEdge = graphModel.CreateEdge(thisPortModel, otherPortModel);
                state.MarkNew(newEdge);
            }

            if (newEdge != null && state.Preferences.GetBool(BoolPref.AutoAlignDraggedEdges))
            {
                state.MarkModelToAutoAlign(newEdge);
            }
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

        public static void DefaultReducer(State state, CreateNodeOnEdgeAction action)
        {
            state.PushUndo(action);

            var edgeInput = action.EdgeModel.ToPort;
            var edgeOutput = action.EdgeModel.FromPort;

            // Instantiate node
            var graphModel = state.GraphModel;

            var position = action.Position - Vector2.up * EdgeActionConfig.nodeOffset;

            List<GUID> guids = action.Guid.HasValue ? new List<GUID> { action.Guid.Value } : null;

            var elementModels = action.SelectedItem.CreateElements.Invoke(
                new GraphNodeCreationData(graphModel, position, guids: guids));

            state.MarkNew(elementModels);

            if (elementModels.Length == 0 || !(elementModels[0] is IInOutPortsNode selectedNodeModel))
                return;

            // Delete old edge
            var deletedModels = graphModel.DeleteEdge(action.EdgeModel);
            state.MarkDeleted(deletedModels);

            // Connect input port
            var inputPortModel = selectedNodeModel.InputsByDisplayOrder.FirstOrDefault(p => p?.PortType == edgeOutput?.PortType);

            if (inputPortModel != null)
            {
                var newEdge = graphModel.CreateEdge(inputPortModel, edgeOutput);
                state.MarkNew(newEdge);
            }

            // Find first matching output type and connect it
            var outputPortModel = selectedNodeModel.GetPortFitToConnectTo(edgeInput);

            if (outputPortModel != null)
            {
                var newEdge = graphModel.CreateEdge(edgeInput, outputPortModel);
                state.MarkNew(newEdge);
            }
        }
    }
}
