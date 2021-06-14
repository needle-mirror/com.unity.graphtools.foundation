using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    static class EdgeCommandConfig
    {
        public const int nodeOffset = 60;
    }

    /// <summary>
    /// Command to create a new edge.
    /// </summary>
    public class CreateEdgeCommand : UndoableCommand
    {
        const string k_UndoString = "Create Edge";

        /// <summary>
        /// Destination port.
        /// </summary>
        public IPortModel ToPortModel;
        /// <summary>
        /// Origin port.
        /// </summary>
        public IPortModel FromPortModel;
        /// <summary>
        /// List of edges to delete.
        /// </summary>
        public IReadOnlyList<IEdgeModel> EdgeModelsToDelete;
        /// <summary>
        /// Directions for which to trigger auto alignment.
        /// </summary>
        public PortDirection PortAlignment;
        /// <summary>
        /// True if the origin node should be itemized.
        /// </summary>
        public bool CreateItemizedNode;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateEdgeCommand" /> class.
        /// </summary>
        public CreateEdgeCommand()
        {
            UndoString = k_UndoString;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateEdgeCommand" /> class.
        /// </summary>
        /// <param name="toPortModel">Destination port.</param>
        /// <param name="fromPortModel">Origin port.</param>
        /// <param name="edgeModelsToDelete">List of edges to delete (sometimes the new edge replaces previous connections).</param>
        /// <param name="portAlignment">Which port's node should be aligned to the other port.</param>
        public CreateEdgeCommand(IPortModel toPortModel, IPortModel fromPortModel,
                                 IReadOnlyList<IEdgeModel> edgeModelsToDelete = null,
                                 PortDirection portAlignment = PortDirection.None)
            : this()
        {
            Assert.IsTrue(toPortModel == null || toPortModel.Direction == PortDirection.Input);
            Assert.IsTrue(fromPortModel == null || fromPortModel.Direction == PortDirection.Output);
            ToPortModel = toPortModel;
            FromPortModel = fromPortModel;
            EdgeModelsToDelete = edgeModelsToDelete;
            PortAlignment = portAlignment;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="graphToolState">The state.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(GraphToolState graphToolState, CreateEdgeCommand command)
        {
            graphToolState.PushUndo(command);

            using (var graphUpdater = graphToolState.GraphViewState.UpdateScope)
            {
                var graphModel = graphToolState.GraphViewState.GraphModel;

                var fromPortModel = command.FromPortModel;
                var toPortModel = command.ToPortModel;

                var edgesToDelete = command.EdgeModelsToDelete ?? new List<IEdgeModel>();

                // Delete previous connections
                if (toPortModel != null && toPortModel.Capacity != PortCapacity.Multi)
                {
                    edgesToDelete = edgesToDelete.Concat(toPortModel.GetConnectedEdges()).ToList();
                }

                if (command.EdgeModelsToDelete != null)
                {
                    graphModel.DeleteEdges(edgesToDelete);
                    graphUpdater.MarkDeleted(edgesToDelete);
                }

                // Auto-itemization preferences will determine if a new node is created or not
                if ((fromPortModel.NodeModel is IConstantNodeModel && graphToolState.Preferences.GetBool(BoolPref.AutoItemizeConstants)) ||
                    (fromPortModel.NodeModel is IVariableNodeModel && graphToolState.Preferences.GetBool(BoolPref.AutoItemizeVariables)))
                {
                    var itemizedNode = graphModel.CreateItemizedNode(EdgeCommandConfig.nodeOffset, ref fromPortModel);
                    if (itemizedNode != null)
                    {
                        graphUpdater.MarkNew(itemizedNode);
                    }
                }

                var edgeModel = graphModel.CreateEdge(toPortModel, fromPortModel);
                graphUpdater.MarkNew(edgeModel);

                if (toPortModel != null && command.PortAlignment.HasFlag(PortDirection.Input))
                    graphUpdater.MarkModelToAutoAlign(toPortModel.NodeModel);
                if (fromPortModel != null && command.PortAlignment.HasFlag(PortDirection.Output))
                    graphUpdater.MarkModelToAutoAlign(fromPortModel.NodeModel);
            }
        }
    }

    /// <summary>
    /// Command to delete one or more edges.
    /// </summary>
    public class DeleteEdgeCommand : ModelCommand<IEdgeModel>
    {
        const string k_UndoStringSingular = "Delete Edge";
        const string k_UndoStringPlural = "Delete Edges";

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteEdgeCommand" /> class.
        /// </summary>
        public DeleteEdgeCommand()
            : base(k_UndoStringSingular) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteEdgeCommand" /> class.
        /// </summary>
        /// <param name="edgesToDelete">The list of edges to delete.</param>
        public DeleteEdgeCommand(IReadOnlyList<IEdgeModel> edgesToDelete)
            : base(k_UndoStringSingular, k_UndoStringPlural, edgesToDelete)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteEdgeCommand" /> class.
        /// </summary>
        /// <param name="edgesToDelete">The list of edges to delete.</param>
        public DeleteEdgeCommand(params IEdgeModel[] edgesToDelete)
            : this((IReadOnlyList<IEdgeModel>)edgesToDelete)
        {
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="graphToolState">The state.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(GraphToolState graphToolState, DeleteEdgeCommand command)
        {
            if (command.Models == null || !command.Models.Any())
                return;

            graphToolState.PushUndo(command);

            using (var graphUpdater = graphToolState.GraphViewState.UpdateScope)
            {
                graphToolState.GraphViewState.GraphModel.DeleteEdges(command.Models);
                graphUpdater.MarkDeleted(command.Models);
            }
        }
    }

    /// <summary>
    /// Command to add a control point on an edge.
    /// </summary>
    public class AddControlPointOnEdgeCommand : UndoableCommand
    {
        /// <summary>
        /// The edge to edit.
        /// </summary>
        public readonly IEditableEdge EdgeModel;
        /// <summary>
        /// Index where to insert the new control point in the list of control points.
        /// </summary>
        public readonly int AtIndex;
        /// <summary>
        /// Position of the new control point.
        /// </summary>
        public readonly Vector2 Position;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddControlPointOnEdgeCommand" /> class.
        /// </summary>
        public AddControlPointOnEdgeCommand()
        {
            UndoString = "Insert Control Point";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AddControlPointOnEdgeCommand" /> class.
        /// </summary>
        /// <param name="edgeModel">The edge to edit.</param>
        /// <param name="atIndex">Index where to insert the new control point in the list of control points.</param>
        /// <param name="position">Position of the new control point.</param>
        public AddControlPointOnEdgeCommand(IEditableEdge edgeModel, int atIndex, Vector2 position) : this()
        {
            EdgeModel = edgeModel;
            AtIndex = atIndex;
            Position = position;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="graphToolState">The state.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(GraphToolState graphToolState, AddControlPointOnEdgeCommand command)
        {
            graphToolState.PushUndo(command);

            using (var graphUpdater = graphToolState.GraphViewState.UpdateScope)
            {
                command.EdgeModel.InsertEdgeControlPoint(command.AtIndex, command.Position, 100);
                graphUpdater.MarkChanged(command.EdgeModel);
            }
        }
    }

    /// <summary>
    /// Command to move an edge's control point or to change its tightness.
    /// </summary>
    public class MoveEdgeControlPointCommand : UndoableCommand
    {
        /// <summary>
        /// The edge to edit.
        /// </summary>
        public readonly IEditableEdge EdgeModel;
        /// <summary>
        /// The index of the control point to move.
        /// </summary>
        public readonly int EdgeIndex;
        /// <summary>
        /// The new position of the control point.
        /// </summary>
        public readonly Vector2 Position;
        /// <summary>
        /// The new tightness value for the control point.
        /// </summary>
        public readonly float Tightness;

        /// <summary>
        /// Initializes a new instance of the <see cref="MoveEdgeControlPointCommand" /> class.
        /// </summary>
        public MoveEdgeControlPointCommand()
        {
            UndoString = "Edit Control Point";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MoveEdgeControlPointCommand" /> class.
        /// </summary>
        /// <param name="edgeModel">The edge to edit.</param>
        /// <param name="edgeIndex">The index of the control point to move.</param>
        /// <param name="position">The new position of the control point.</param>
        /// <param name="tightness">The new tightness value for the control point.</param>
        public MoveEdgeControlPointCommand(IEditableEdge edgeModel, int edgeIndex, Vector2 position, float tightness) : this()
        {
            EdgeModel = edgeModel;
            EdgeIndex = edgeIndex;
            Position = position;
            Tightness = tightness;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="graphToolState">The state.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(GraphToolState graphToolState, MoveEdgeControlPointCommand command)
        {
            graphToolState.PushUndo(command);

            using (var graphUpdater = graphToolState.GraphViewState.UpdateScope)
            {
                command.EdgeModel.ModifyEdgeControlPoint(command.EdgeIndex, command.Position, command.Tightness);
                graphUpdater.MarkChanged(command.EdgeModel);
            }
        }
    }

    /// <summary>
    /// Command to remove a control point on an edge.
    /// </summary>
    public class RemoveEdgeControlPointCommand : UndoableCommand
    {
        /// <summary>
        /// The edge on which to remove a control point.
        /// </summary>
        public readonly IEditableEdge EdgeModel;
        /// <summary>
        /// The index of the control point to remove.
        /// </summary>
        public readonly int EdgeIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoveEdgeControlPointCommand"/> class.
        /// </summary>
        public RemoveEdgeControlPointCommand()
        {
            UndoString = "Remove Control Point";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoveEdgeControlPointCommand"/> class.
        /// </summary>
        /// <param name="edgeModel">The edge on which to remove a control point.</param>
        /// <param name="edgeIndex">The index of the control point to remove.</param>
        public RemoveEdgeControlPointCommand(IEditableEdge edgeModel, int edgeIndex) : this()
        {
            EdgeModel = edgeModel;
            EdgeIndex = edgeIndex;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="graphToolState">The state.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(GraphToolState graphToolState, RemoveEdgeControlPointCommand command)
        {
            graphToolState.PushUndo(command);

            using (var graphUpdater = graphToolState.GraphViewState.UpdateScope)
            {
                command.EdgeModel.RemoveEdgeControlPoint(command.EdgeIndex);
                graphUpdater.MarkChanged(command.EdgeModel);
            }
        }
    }

    /// <summary>
    /// Command to change the edit mode of an edge.
    /// </summary>
    public class SetEdgeEditModeCommand : UndoableCommand
    {
        /// <summary>
        /// The edge to modify.
        /// </summary>
        public readonly IEditableEdge EdgeModel;
        /// <summary>
        /// True if the edge should go enter edit mode, false if it should exit edit mode.
        /// </summary>
        public readonly bool Value;

        /// <summary>
        /// Initializes a new instance of the <see cref="SetEdgeEditModeCommand"/> class.
        /// </summary>
        public SetEdgeEditModeCommand()
        {
            UndoString = "Set Edge Edit Mode";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SetEdgeEditModeCommand"/> class.
        /// </summary>
        /// <param name="edgeModel">The edge to modify.</param>
        /// <param name="value">True if the edge should go enter edit mode, false if it should exit edit mode.</param>
        public SetEdgeEditModeCommand(IEditableEdge edgeModel, bool value) : this()
        {
            EdgeModel = edgeModel;
            Value = value;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="graphToolState">The state.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(GraphToolState graphToolState, SetEdgeEditModeCommand command)
        {
            graphToolState.PushUndo(command);

            using (var graphUpdater = graphToolState.GraphViewState.UpdateScope)
            {
                command.EdgeModel.EditMode = command.Value;
                graphUpdater.MarkChanged(command.EdgeModel);
            }
        }
    }

    /// <summary>
    /// Command to change the order of an edge.
    /// </summary>
    public class ReorderEdgeCommand : UndoableCommand
    {
        /// <summary>
        /// Reorder operations.
        /// </summary>
        public enum ReorderType
        {
            /// <summary>
            /// Make the edge the first edge.
            /// </summary>
            MoveFirst,
            /// <summary>
            /// Move the edge one position towards the beginning.
            /// </summary>
            MoveUp,
            /// <summary>
            /// Move the edge one position towards the end.
            /// </summary>
            MoveDown,
            /// <summary>
            /// Make the edge the last edge.
            /// </summary>
            MoveLast
        }

        /// <summary>
        /// The edge to reorder.
        /// </summary>
        public readonly IEdgeModel EdgeModel;
        /// <summary>
        /// The reorder operation to apply.
        /// </summary>
        public readonly ReorderType Type;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReorderEdgeCommand"/> class.
        /// </summary>
        public ReorderEdgeCommand()
        {
            UndoString = "Reorder Edge";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReorderEdgeCommand"/> class.
        /// </summary>
        /// <param name="edgeModel">The edge to reorder.</param>
        /// <param name="type">The reorder operation to apply.</param>
        public ReorderEdgeCommand(IEdgeModel edgeModel, ReorderType type) : this()
        {
            EdgeModel = edgeModel;
            Type = type;

            switch (Type)
            {
                case ReorderType.MoveFirst:
                    UndoString = "Move Edge First";
                    break;
                case ReorderType.MoveUp:
                    UndoString = "Move Edge Up";
                    break;
                case ReorderType.MoveDown:
                    UndoString = "Move Edge Down";
                    break;
                case ReorderType.MoveLast:
                    UndoString = "Move Edge Last";
                    break;
            }
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="graphToolState">The state.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(GraphToolState graphToolState, ReorderEdgeCommand command)
        {
            if (command.EdgeModel?.FromPort is IReorderableEdgesPortModel fromPort && fromPort.HasReorderableEdges)
            {
                var siblingEdges = fromPort.GetConnectedEdges().ToList();
                var siblingEdgesCount = siblingEdges.Count;
                if (siblingEdgesCount > 1)
                {
                    var index = siblingEdges.IndexOf(command.EdgeModel);
                    Action<IEdgeModel> reorderAction = null;
                    switch (command.Type)
                    {
                        case ReorderType.MoveFirst when index > 0:
                            reorderAction = fromPort.MoveEdgeFirst;
                            break;
                        case ReorderType.MoveUp when index > 0:
                            reorderAction = fromPort.MoveEdgeUp;
                            break;
                        case ReorderType.MoveDown when index < siblingEdgesCount - 1:
                            reorderAction = fromPort.MoveEdgeDown;
                            break;
                        case ReorderType.MoveLast when index < siblingEdgesCount - 1:
                            reorderAction = fromPort.MoveEdgeLast;
                            break;
                    }

                    if (reorderAction != null)
                    {
                        graphToolState.PushUndo(command);

                        using (var graphUpdater = graphToolState.GraphViewState.UpdateScope)
                        {
                            reorderAction(command.EdgeModel);

                            graphUpdater.MarkChanged(siblingEdges);
                            graphUpdater.MarkChanged(fromPort.NodeModel);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Command to insert a node in the middle of an edge.
    /// </summary>
    public class SplitEdgeAndInsertExistingNodeCommand : UndoableCommand
    {
        public readonly IEdgeModel EdgeModel;
        public readonly IInputOutputPortsNodeModel NodeModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="SplitEdgeAndInsertExistingNodeCommand"/> class.
        /// </summary>
        public SplitEdgeAndInsertExistingNodeCommand()
        {
            UndoString = "Insert Node On Edge";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SplitEdgeAndInsertExistingNodeCommand"/> class.
        /// </summary>
        /// <param name="edgeModel">The edge on which to insert a node.</param>
        /// <param name="nodeModel">The node to insert.</param>
        public SplitEdgeAndInsertExistingNodeCommand(IEdgeModel edgeModel, IInputOutputPortsNodeModel nodeModel) : this()
        {
            EdgeModel = edgeModel;
            NodeModel = nodeModel;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="graphToolState">The state.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(GraphToolState graphToolState, SplitEdgeAndInsertExistingNodeCommand command)
        {
            Assert.IsTrue(command.NodeModel.InputsById.Count > 0);
            Assert.IsTrue(command.NodeModel.OutputsById.Count > 0);

            graphToolState.PushUndo(command);

            using (var graphUpdater = graphToolState.GraphViewState.UpdateScope)
            {
                var graphModel = graphToolState.GraphViewState.GraphModel;
                var edgeInput = command.EdgeModel.ToPort;
                var edgeOutput = command.EdgeModel.FromPort;
                var deletedModels = graphModel.DeleteEdge(command.EdgeModel);
                var edge1 = graphModel.CreateEdge(edgeInput, command.NodeModel.OutputsByDisplayOrder.First(p => p?.PortType == edgeInput?.PortType));
                var edge2 = graphModel.CreateEdge(command.NodeModel.InputsByDisplayOrder.First(p => p?.PortType == edgeOutput?.PortType), edgeOutput);

                graphUpdater.MarkDeleted(deletedModels);
                graphUpdater.MarkNew(edge1);
                graphUpdater.MarkNew(edge2);
            }
        }
    }

    /// <summary>
    /// Command to convert edges to portal nodes.
    /// </summary>
    public class ConvertEdgesToPortalsCommand : UndoableCommand
    {
        const string k_UndoStringSingular = "Convert Edge to Portal";
        const string k_UndoStringPlural = "Convert Edges to Portals";

        static readonly Vector2 k_EntryPortalBaseOffset = Vector2.right * 75;
        static readonly Vector2 k_ExitPortalBaseOffset = Vector2.left * 250;
        const int k_PortalHeight = 24;

        /// <summary>
        /// Data describing which edge to transform and the position of the portals.
        /// </summary>
        public List<(IEdgeModel edge, Vector2 startPortPos, Vector2 endPortPos)> EdgeData;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConvertEdgesToPortalsCommand"/> class.
        /// </summary>
        public ConvertEdgesToPortalsCommand()
        {
            UndoString = k_UndoStringSingular;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConvertEdgesToPortalsCommand"/> class.
        /// </summary>
        /// <param name="edgeData">A list of tuple, each tuple containing the edge to convert, the position of the entry portal node and the position of the exit portal node.</param>
        public ConvertEdgesToPortalsCommand(IReadOnlyList<(IEdgeModel, Vector2, Vector2)> edgeData) : this()
        {
            EdgeData = edgeData?.ToList();
            UndoString = (EdgeData?.Count ?? 0) <= 1 ? k_UndoStringSingular : k_UndoStringPlural;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="graphToolState">The state.</param>
        /// <param name="command">The command.</param>
        // TODO JOCE: Move to GraphView or something. We should be able to create from edge without a command handler (for tests, for example)
        public static void DefaultCommandHandler(GraphToolState graphToolState, ConvertEdgesToPortalsCommand command)
        {
            var graphModel = graphToolState.GraphViewState.GraphModel;

            if (command.EdgeData == null || !command.EdgeData.Any())
                return;

            graphToolState.PushUndo(command);

            using (var updater = graphToolState.GraphViewState.UpdateScope)
            {
                var existingPortalEntries = new Dictionary<IPortModel, IEdgePortalEntryModel>();
                var existingPortalExits = new Dictionary<IPortModel, List<IEdgePortalExitModel>>();

                foreach (var edgeModel in command.EdgeData)
                    ConvertEdgeToPortals(edgeModel);

                // Adjust placement in case of multiple incoming exit portals so they don't overlap
                foreach (var portalList in existingPortalExits.Values.Where(l => l.Count > 1))
                {
                    var cnt = portalList.Count;
                    bool isEven = cnt % 2 == 0;
                    int offset = isEven ? k_PortalHeight / 2 : 0;
                    for (int i = (cnt - 1) / 2; i >= 0; i--)
                    {
                        portalList[i].Position = new Vector2(portalList[i].Position.x, portalList[i].Position.y - offset);
                        portalList[cnt - 1 - i].Position = new Vector2(portalList[cnt - 1 - i].Position.x, portalList[cnt - 1 - i].Position.y + offset);
                        offset += k_PortalHeight;
                    }
                }

                var edgesToDelete = command.EdgeData.Select(d => d.edge).ToList();
                graphModel.DeleteEdges(edgesToDelete);
                updater.MarkDeleted(edgesToDelete);

                void ConvertEdgeToPortals((IEdgeModel edgeModel, Vector2 startPos, Vector2 endPos) data)
                {
                    // Only a single portal per output port. Don't recreate if we already created one.
                    var outputPortModel = data.edgeModel.FromPort;
                    IEdgePortalEntryModel portalEntry = null;
                    if (outputPortModel != null && !existingPortalEntries.TryGetValue(data.edgeModel.FromPort, out portalEntry))
                    {
                        portalEntry = graphModel.CreateEntryPortalFromEdge(data.edgeModel);
                        existingPortalEntries[outputPortModel] = portalEntry;
                        updater.MarkNew(portalEntry);

                        if (!(outputPortModel.NodeModel is IInputOutputPortsNodeModel nodeModel))
                            return;

                        portalEntry.Position = data.startPos + k_EntryPortalBaseOffset;

                        // y offset based on port order. hurgh.
                        var idx = nodeModel.OutputsByDisplayOrder.IndexOfInternal(outputPortModel);
                        portalEntry.Position += Vector2.down * (k_PortalHeight * idx + 16); // Fudgy.

                        string portalName;
                        if (nodeModel is IConstantNodeModel constantNodeModel)
                            portalName = constantNodeModel.Type.FriendlyName();
                        else
                        {
                            portalName = (nodeModel as IHasTitle)?.Title ?? "";
                            var portName = (outputPortModel as IHasTitle)?.Title ?? "";
                            if (!string.IsNullOrEmpty(portName))
                                portalName += " - " + portName;
                        }

                        portalEntry.DeclarationModel = graphModel.CreateGraphPortalDeclaration(portalName);
                        updater.MarkNew(portalEntry.DeclarationModel);

                        var newEntryEdge = graphModel.CreateEdge(portalEntry.InputPort, outputPortModel);
                        updater.MarkNew(newEntryEdge);
                    }

                    // We can have multiple portals on input ports however
                    if (!existingPortalExits.TryGetValue(data.edgeModel.ToPort, out var portalExits))
                    {
                        portalExits = new List<IEdgePortalExitModel>();
                        existingPortalExits[data.edgeModel.ToPort] = portalExits;
                    }

                    IEdgePortalExitModel portalExit;
                    var inputPortModel = data.edgeModel.ToPort;
                    portalExit = graphModel.CreateExitPortalFromEdge(data.edgeModel);
                    portalExits.Add(portalExit);
                    updater.MarkNew(portalExit);

                    portalExit.Position = data.endPos + k_ExitPortalBaseOffset;
                    {
                        if (data.edgeModel.ToPort.NodeModel is IInputOutputPortsNodeModel nodeModel)
                        {
                            // y offset based on port order. hurgh.
                            var idx = nodeModel.InputsByDisplayOrder.IndexOfInternal(inputPortModel);
                            portalExit.Position += Vector2.down * (k_PortalHeight * idx + 16); // Fudgy.
                        }
                    }

                    portalExit.DeclarationModel = portalEntry?.DeclarationModel;

                    var newExitEdge = graphModel.CreateEdge(inputPortModel, portalExit.OutputPort);
                    updater.MarkNew(newExitEdge);
                }
            }
        }
    }
}
