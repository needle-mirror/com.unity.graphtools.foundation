using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Command to create a node from a <see cref="GraphNodeModelSearcherItem"/>.
    /// </summary>
    public class CreateNodeFromSearcherCommand : Command
    {
        /// <summary>
        /// The position where to create the node.
        /// </summary>
        public Vector2 Position;

        /// <summary>
        /// The searcher item representing the node to create.
        /// </summary>
        public GraphNodeModelSearcherItem SelectedItem;

        /// <summary>
        /// The SerializableGUID to assign to the newly created item.
        /// </summary>
        public SerializableGUID Guid;

        /// <summary>
        /// Initializes a new CreateNodeFromSearcherCommand.
        /// </summary>
        public CreateNodeFromSearcherCommand()
        {
            UndoString = "Create Node";
        }

        /// <summary>
        /// Initializes a new CreateNodeFromSearcherCommand.
        /// </summary>
        /// <param name="position">The position where to create the node.</param>
        /// <param name="selectedItem">The searcher item representing the node to create.</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item. If none is provided, a new
        /// SerializableGUID will be generated for it.</param>
        public CreateNodeFromSearcherCommand(Vector2 position,
                                             GraphNodeModelSearcherItem selectedItem,
                                             SerializableGUID guid = default) : this()
        {
            Position = position;
            SelectedItem = selectedItem;
            Guid = guid.Valid ? guid : SerializableGUID.Generate();
        }

        /// <summary>
        /// Default command handler for CreateNodeFromSearcherCommand.
        /// </summary>
        /// <param name="graphToolState">The current graph tool state.</param>
        /// <param name="command">The command to handle.</param>
        public static void DefaultCommandHandler(GraphToolState graphToolState, CreateNodeFromSearcherCommand command)
        {
            graphToolState.PushUndo(command);

            var newModels = command.SelectedItem.CreateElements.Invoke(
                new GraphNodeCreationData(graphToolState.GraphModel, command.Position, guid: command.Guid));

            graphToolState.MarkNew(newModels);
        }
    }

    public class CreateNodeFromPortCommand : Command
    {
        public IPortModel PortModel;
        public Vector2 Position;
        public GraphNodeModelSearcherItem SelectedItem;
        public IReadOnlyList<IEdgeModel> EdgesToDelete;
        public bool ItemizeSourceNode;

        public CreateNodeFromPortCommand()
        {
            UndoString = "Create Node";
        }

        public CreateNodeFromPortCommand(IPortModel portModel, Vector2 position, GraphNodeModelSearcherItem selectedItem,
                                         IReadOnlyList<IEdgeModel> edgesToDelete = null, bool itemizeSourceNode = true) : this()
        {
            PortModel = portModel;
            Position = position;
            SelectedItem = selectedItem;
            EdgesToDelete = edgesToDelete;
            ItemizeSourceNode = itemizeSourceNode;
        }

        public static void DefaultCommandHandler(GraphToolState graphToolState, CreateNodeFromPortCommand command)
        {
            graphToolState.PushUndo(command);

            var graphModel = graphToolState.GraphModel;
            if (command.EdgesToDelete != null)
            {
                var deletedModels = graphModel.DeleteEdges(command.EdgesToDelete);
                graphToolState.MarkDeleted(deletedModels);
            }

            var position = command.Position - Vector2.up * EdgeCommandConfig.nodeOffset;
            var elementModels = command.SelectedItem.CreateElements.Invoke(
                new GraphNodeCreationData(graphModel, position));

            graphToolState.MarkNew(elementModels);

            if (!elementModels.Any() || !(elementModels[0] is IPortNode selectedNodeModel))
                return;

            var otherPortModel = selectedNodeModel.GetPortFitToConnectTo(command.PortModel);

            if (otherPortModel == null)
                return;

            var thisPortModel = command.PortModel;

            IEdgeModel newEdge;
            if (thisPortModel.Direction == Direction.Output)
            {
                if (command.ItemizeSourceNode)
                {
                    graphModel.CreateItemizedNode(graphToolState, EdgeCommandConfig.nodeOffset, ref thisPortModel);
                    graphToolState.RequestUIRebuild();
                }

                newEdge = graphModel.CreateEdge(otherPortModel, thisPortModel);
                graphToolState.MarkNew(newEdge);
            }
            else
            {
                newEdge = graphModel.CreateEdge(thisPortModel, otherPortModel);
                graphToolState.MarkNew(newEdge);
            }

            if (newEdge != null && graphToolState.Preferences.GetBool(BoolPref.AutoAlignDraggedEdges))
            {
                graphToolState.MarkModelToAutoAlign(newEdge);
            }
        }
    }

    /// <summary>
    /// Command to create a node from a <see cref="GraphNodeModelSearcherItem"/> and insert in on an edge.
    /// </summary>
    public class CreateNodeOnEdgeCommand : Command
    {
        /// <summary>
        /// The edge model on which to insert the newly created node.
        /// </summary>
        public IEdgeModel EdgeModel;

        /// <summary>
        /// The position where to create the node.
        /// </summary>
        public Vector2 Position;

        /// <summary>
        /// The searcher item representing the node to create.
        /// </summary>
        public GraphNodeModelSearcherItem SelectedItem;

        /// <summary>
        /// The SerializableGUID to assign to the newly created item.
        /// </summary>
        public SerializableGUID Guid;

        /// <summary>
        /// Initializes a new CreateNodeFromSearcherCommand.
        /// </summary>
        public CreateNodeOnEdgeCommand()
        {
            UndoString = "Create Node";
        }

        /// <summary>
        /// Initializes a new CreateNodeFromSearcherCommand.
        /// </summary>
        /// <param name="edgeModel">The edge model on which to insert the newly created node.</param>
        /// <param name="position">The position where to create the node.</param>
        /// <param name="selectedItem">The searcher item representing the node to create.</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item. If none is provided, a new
        /// SerializableGUID will be generated for it.</param>
        public CreateNodeOnEdgeCommand(IEdgeModel edgeModel, Vector2 position,
                                       GraphNodeModelSearcherItem selectedItem, SerializableGUID guid = default) : this()
        {
            EdgeModel = edgeModel;
            Position = position;
            SelectedItem = selectedItem;
            Guid = guid.Valid ? guid : SerializableGUID.Generate();
        }

        /// <summary>
        /// Default command handler for CreateNodeOnEdgeCommand.
        /// </summary>
        /// <param name="graphToolState">The current graph tool state.</param>
        /// <param name="command">The command to handle.</param>
        public static void DefaultCommandHandler(GraphToolState graphToolState, CreateNodeOnEdgeCommand command)
        {
            graphToolState.PushUndo(command);

            var edgeInput = command.EdgeModel.ToPort;
            var edgeOutput = command.EdgeModel.FromPort;

            // Instantiate node
            var graphModel = graphToolState.GraphModel;

            var position = command.Position - Vector2.up * EdgeCommandConfig.nodeOffset;

            var elementModels = command.SelectedItem.CreateElements.Invoke(
                new GraphNodeCreationData(graphModel, position, guid: command.Guid));

            graphToolState.MarkNew(elementModels);

            if (elementModels.Length == 0 || !(elementModels[0] is IInOutPortsNode selectedNodeModel))
                return;

            // Delete old edge
            var deletedModels = graphModel.DeleteEdge(command.EdgeModel);
            graphToolState.MarkDeleted(deletedModels);

            // Connect input port
            var inputPortModel = selectedNodeModel.InputsByDisplayOrder.FirstOrDefault(p => p?.PortType == edgeOutput?.PortType);

            if (inputPortModel != null)
            {
                var newEdge = graphModel.CreateEdge(inputPortModel, edgeOutput);
                graphToolState.MarkNew(newEdge);
            }

            // Find first matching output type and connect it
            var outputPortModel = selectedNodeModel.GetPortFitToConnectTo(edgeInput);

            if (outputPortModel != null)
            {
                var newEdge = graphModel.CreateEdge(edgeInput, outputPortModel);
                graphToolState.MarkNew(newEdge);
            }
        }
    }
}
