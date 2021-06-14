using System.Collections.Generic;
using System.Linq;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Command to create a node from a <see cref="GraphNodeModelSearcherItem"/>.
    /// </summary>
    public class CreateNodeFromSearcherCommand : UndoableCommand
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

            using (var graphUpdater = graphToolState.GraphViewState.UpdateScope)
            {
                var newModels = command.SelectedItem.CreateElements.Invoke(
                    new GraphNodeCreationData(graphToolState.GraphViewState.GraphModel, command.Position, guid: command.Guid));

                graphUpdater.MarkNew(newModels);
            }
        }
    }

    /// <summary>
    /// Command to create a node from a <see cref="GraphNodeModelSearcherItem"/> and to connect it to existing ports.
    /// </summary>
    public class CreateNodeFromPortCommand : UndoableCommand
    {
        /// <summary>
        /// The ports to which to connect the new node.
        /// </summary>
        public IReadOnlyList<IPortModel> PortModels;
        /// <summary>
        /// The position where to create the node.
        /// </summary>
        public Vector2 Position;
        /// <summary>
        /// The searcher item representing the node to create.
        /// </summary>
        public GraphNodeModelSearcherItem SelectedItem;
        /// <summary>
        /// Edges to delete.
        /// </summary>
        public IReadOnlyList<IEdgeModel> EdgesToDelete;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateNodeFromPortCommand"/> class.
        /// </summary>
        public CreateNodeFromPortCommand()
        {
            UndoString = "Create Node";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateNodeFromPortCommand"/> class.
        /// </summary>
        /// <param name="portModel">The ports to which to connect the new node.</param>
        /// <param name="position">The position where to create the node.</param>
        /// <param name="selectedItem">The searcher item representing the node to create.</param>
        /// <param name="edgesToDelete">Edges to delete.</param>
        public CreateNodeFromPortCommand(IReadOnlyList<IPortModel> portModel, Vector2 position, GraphNodeModelSearcherItem selectedItem,
                                         IReadOnlyList<IEdgeModel> edgesToDelete = null) : this()
        {
            PortModels = portModel;
            Position = position;
            SelectedItem = selectedItem;
            EdgesToDelete = edgesToDelete;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="graphToolState">The state.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(GraphToolState graphToolState, CreateNodeFromPortCommand command)
        {
            graphToolState.PushUndo(command);

            using (var graphUpdater = graphToolState.GraphViewState.UpdateScope)
            {
                var graphModel = graphToolState.GraphViewState.GraphModel;

                var position = command.Position - Vector2.up * EdgeCommandConfig.nodeOffset;
                var elementModels = command.SelectedItem.CreateElements.Invoke(
                    new GraphNodeCreationData(graphModel, position));

                graphUpdater.MarkNew(elementModels);

                if (!elementModels.Any() || !(elementModels[0] is IPortNodeModel selectedNodeModel))
                    return;

                var otherPortModel = selectedNodeModel.GetPortFitToConnectTo(command.PortModels.First());

                if (otherPortModel == null)
                    return;

                foreach (var portModel in command.PortModels)
                {
                    var thisPortModel = portModel;
                    IEdgeModel newEdge;
                    if (thisPortModel.Direction == PortDirection.Output)
                    {
                        if ((thisPortModel.NodeModel is IConstantNodeModel && graphToolState.Preferences.GetBool(BoolPref.AutoItemizeConstants)) ||
                            (thisPortModel.NodeModel is IVariableNodeModel && graphToolState.Preferences.GetBool(BoolPref.AutoItemizeVariables)))
                        {
                            var newNode = graphModel.CreateItemizedNode(EdgeCommandConfig.nodeOffset, ref thisPortModel);
                            graphUpdater.MarkNew(newNode);
                        }

                        newEdge = graphModel.CreateEdge(otherPortModel, thisPortModel);
                        graphUpdater.MarkNew(newEdge);
                    }
                    else
                    {
                        newEdge = graphModel.CreateEdge(thisPortModel, otherPortModel);
                        graphUpdater.MarkNew(newEdge);
                    }

                    if (command.EdgesToDelete != null)
                    {
                        var deletedModels = graphModel.DeleteEdges(command.EdgesToDelete);
                        graphUpdater.MarkDeleted(deletedModels);
                    }

                    if (newEdge != null && graphToolState.Preferences.GetBool(BoolPref.AutoAlignDraggedEdges))
                    {
                        graphUpdater.MarkModelToAutoAlign(newEdge);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Command to create a node from a <see cref="GraphNodeModelSearcherItem"/> and insert in on an edge.
    /// </summary>
    public class CreateNodeOnEdgeCommand : UndoableCommand
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

            using (var graphUpdater = graphToolState.GraphViewState.UpdateScope)
            {
                var edgeInput = command.EdgeModel.ToPort;
                var edgeOutput = command.EdgeModel.FromPort;

                // Instantiate node
                var graphModel = graphToolState.GraphViewState.GraphModel;

                var position = command.Position - Vector2.up * EdgeCommandConfig.nodeOffset;

                var elementModels = command.SelectedItem.CreateElements.Invoke(
                    new GraphNodeCreationData(graphModel, position, guid: command.Guid));

                graphUpdater.MarkNew(elementModels);

                if (elementModels.Length == 0 || !(elementModels[0] is IInputOutputPortsNodeModel selectedNodeModel))
                    return;

                // Delete old edge
                var deletedModels = graphModel.DeleteEdge(command.EdgeModel);
                graphUpdater.MarkDeleted(deletedModels);

                // Connect input port
                var inputPortModel = selectedNodeModel.InputsByDisplayOrder.FirstOrDefault(p => p?.PortType == edgeOutput?.PortType);

                if (inputPortModel != null)
                {
                    var newEdge = graphModel.CreateEdge(inputPortModel, edgeOutput);
                    graphUpdater.MarkNew(newEdge);
                }

                // Find first matching output type and connect it
                var outputPortModel = selectedNodeModel.GetPortFitToConnectTo(edgeInput);

                if (outputPortModel != null)
                {
                    var newEdge = graphModel.CreateEdge(edgeInput, outputPortModel);
                    graphUpdater.MarkNew(newEdge);
                }
            }
        }
    }
}
