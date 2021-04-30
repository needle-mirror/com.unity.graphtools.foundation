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

    public class CreateEdgeCommand : UndoableCommand
    {
        const string k_UndoString = "Create Edge";

        public IPortModel ToPortModel;
        public IPortModel FromPortModel;
        public IReadOnlyList<IEdgeModel> EdgeModelsToDelete;
        public Direction PortAlignment;
        public bool CreateItemizedNode;

        public CreateEdgeCommand()
        {
            UndoString = k_UndoString;
        }

        public CreateEdgeCommand(IPortModel toPortModel, IPortModel fromPortModel,
                                 IReadOnlyList<IEdgeModel> edgeModelsToDelete = null,
                                 Direction portAlignment = Direction.None,
                                 bool createItemizedNode = true)
            : this()
        {
            Assert.IsTrue(toPortModel == null || toPortModel.Direction == Direction.Input);
            Assert.IsTrue(fromPortModel == null || fromPortModel.Direction == Direction.Output);
            ToPortModel = toPortModel;
            FromPortModel = fromPortModel;
            EdgeModelsToDelete = edgeModelsToDelete;
            PortAlignment = portAlignment;
            CreateItemizedNode = createItemizedNode;
        }

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

                if (command.CreateItemizedNode)
                {
                    var newNode = graphModel.CreateItemizedNode(EdgeCommandConfig.nodeOffset, ref fromPortModel);
                    graphUpdater.MarkNew(newNode);
                }

                var edgeModel = graphModel.CreateEdge(toPortModel, fromPortModel);
                graphUpdater.MarkNew(edgeModel);

                if (command.PortAlignment.HasFlag(Direction.Input))
                    graphUpdater.MarkModelToAutoAlign(toPortModel.NodeModel);
                if (command.PortAlignment.HasFlag(Direction.Output))
                    graphUpdater.MarkModelToAutoAlign(fromPortModel.NodeModel);
            }
        }
    }

    public class DeleteEdgeCommand : ModelCommand<IEdgeModel>
    {
        const string k_UndoStringSingular = "Delete Edge";
        const string k_UndoStringPlural = "Delete Edges";

        public DeleteEdgeCommand()
            : base(k_UndoStringSingular) { }

        public DeleteEdgeCommand(IReadOnlyList<IEdgeModel> edgesToDelete)
            : base(k_UndoStringSingular, k_UndoStringPlural, edgesToDelete)
        {
        }

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

    public class AddControlPointOnEdgeCommand : UndoableCommand
    {
        public readonly IEditableEdge EdgeModel;
        public readonly int AtIndex;
        public readonly Vector2 Position;

        public AddControlPointOnEdgeCommand()
        {
            UndoString = "Insert Control Point";
        }

        public AddControlPointOnEdgeCommand(IEditableEdge edgeModel, int atIndex, Vector2 position) : this()
        {
            EdgeModel = edgeModel;
            AtIndex = atIndex;
            Position = position;
        }

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

    public class MoveEdgeControlPointCommand : UndoableCommand
    {
        public readonly IEditableEdge EdgeModel;
        public readonly int EdgeIndex;
        public readonly Vector2 NewPosition;
        public readonly float NewTightness;

        public MoveEdgeControlPointCommand()
        {
            UndoString = "Edit Control Point";
        }

        public MoveEdgeControlPointCommand(IEditableEdge edgeModel, int edgeIndex, Vector2 newPosition, float newTightness) : this()
        {
            EdgeModel = edgeModel;
            EdgeIndex = edgeIndex;
            NewPosition = newPosition;
            NewTightness = newTightness;
        }

        public static void DefaultCommandHandler(GraphToolState graphToolState, MoveEdgeControlPointCommand command)
        {
            graphToolState.PushUndo(command);

            using (var graphUpdater = graphToolState.GraphViewState.UpdateScope)
            {
                command.EdgeModel.ModifyEdgeControlPoint(command.EdgeIndex, command.NewPosition, command.NewTightness);
                graphUpdater.MarkChanged(command.EdgeModel);
            }
        }
    }

    public class RemoveEdgeControlPointCommand : UndoableCommand
    {
        public readonly IEditableEdge EdgeModel;
        public readonly int EdgeIndex;

        public RemoveEdgeControlPointCommand()
        {
            UndoString = "Remove Control Point";
        }

        public RemoveEdgeControlPointCommand(IEditableEdge edgeModel, int edgeIndex) : this()
        {
            EdgeModel = edgeModel;
            EdgeIndex = edgeIndex;
        }

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

    public class SetEdgeEditModeCommand : UndoableCommand
    {
        public readonly IEditableEdge EdgeModel;
        public readonly bool Value;

        public SetEdgeEditModeCommand()
        {
            UndoString = "Set Edge Edit Mode";
        }

        public SetEdgeEditModeCommand(IEditableEdge edgeModel, bool value) : this()
        {
            EdgeModel = edgeModel;
            Value = value;
        }

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

    public class ReorderEdgeCommand : UndoableCommand
    {
        public enum ReorderType
        {
            MoveFirst,
            MoveUp,
            MoveDown,
            MoveLast
        }

        public readonly IEdgeModel EdgeModel;
        public readonly ReorderType Type;

        public ReorderEdgeCommand()
        {
            UndoString = "Reorder Edge";
        }

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

    public class SplitEdgeAndInsertExistingNodeCommand : UndoableCommand
    {
        public readonly IEdgeModel EdgeModel;
        public readonly IInputOutputPortsNodeModel NodeModel;

        public SplitEdgeAndInsertExistingNodeCommand()
        {
            UndoString = "Insert Node On Edge";
        }

        public SplitEdgeAndInsertExistingNodeCommand(IEdgeModel edgeModel, IInputOutputPortsNodeModel nodeModel) : this()
        {
            EdgeModel = edgeModel;
            NodeModel = nodeModel;
        }

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

    public class ConvertEdgesToPortalsCommand : UndoableCommand
    {
        const string k_UndoStringSingular = "Convert Edge to Portal";
        const string k_UndoStringPlural = "Convert Edges to Portals";

        static readonly Vector2 k_EntryPortalBaseOffset = Vector2.right * 75;
        static readonly Vector2 k_ExitPortalBaseOffset = Vector2.left * 250;
        const int k_PortalHeight = 24;

        public (IEdgeModel edge, Vector2 startPortPos, Vector2 endPortPos)[] EdgeData;

        public ConvertEdgesToPortalsCommand()
        {
            UndoString = k_UndoStringSingular;
        }

        public ConvertEdgesToPortalsCommand(IReadOnlyList<(IEdgeModel, Vector2, Vector2)> edgeData) : this()
        {
            EdgeData = edgeData?.ToArray();
            UndoString = (EdgeData?.Length ?? 0) <= 1 ? k_UndoStringSingular : k_UndoStringPlural;
        }

        // TODO JOCE: Move to GraphView or something. We should be able to create from edge without a command handler (for tests, for example)
        public static void DefaultCommandHandler(GraphToolState graphToolState, ConvertEdgesToPortalsCommand command)
        {
            var graphModel = graphToolState.GraphViewState.GraphModel;

            if (command.EdgeData == null)
                return;

            var edgeData = command.EdgeData.ToList();
            if (!edgeData.Any())
                return;

            graphToolState.PushUndo(command);

            using (var updater = graphToolState.GraphViewState.UpdateScope)
            {
                var existingPortalEntries = new Dictionary<IPortModel, IEdgePortalEntryModel>();
                var existingPortalExits = new Dictionary<IPortModel, List<IEdgePortalExitModel>>();

                foreach (var edgeModel in edgeData)
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

                var edgesToDelete = edgeData.Select(d => d.edge).ToList();
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
                        var idx = nodeModel.OutputsByDisplayOrder.IndexOf(outputPortModel);
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
                            var idx = nodeModel.InputsByDisplayOrder.IndexOf(inputPortModel);
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

    public class TogglePortsCommand : UndoableCommand
    {
        public TogglePortsCommand()
        {
            UndoString = "Toggle Ports";
        }

        public static void DefaultCommandHandler(GraphToolState graphToolState, TogglePortsCommand command)
        {
            graphToolState.PushUndo(command);

            using (var graphUpdater = graphToolState.GraphViewState.UpdateScope)
            {
                var changedPorts = new List<IPortModel>();
                foreach (var node in graphToolState.GraphViewState.GraphModel.NodeModels.OfType<IPortNodeModel>())
                {
                    foreach (var port in node.Ports)
                    {
                        if (port.Orientation == Orientation.Horizontal)
                            port.Orientation = Orientation.Vertical;
                        else
                            port.Orientation = Orientation.Horizontal;
                        changedPorts.Add(port);
                    }
                }

                graphUpdater.MarkChanged(changedPorts);
            }
        }
    }

    public class ToggleEdgePortsCommand : UndoableCommand
    {
        public readonly IEdgeModel[] EdgeModels;

        public ToggleEdgePortsCommand(IEdgeModel[] edgeModels)
        {
            UndoString = "Toggle Edge Ports";
            EdgeModels = edgeModels;
        }

        public static void DefaultCommandHandler(GraphToolState graphToolState, ToggleEdgePortsCommand command)
        {
            graphToolState.PushUndo(command);

            using (var graphUpdater = graphToolState.GraphViewState.UpdateScope)
            {
                var changedPorts = new List<IPortModel>();

                foreach (var edgeModel in command.EdgeModels)
                {
                    foreach (var port in new[] { edgeModel.FromPort, edgeModel.ToPort })
                    {
                        if (port.Orientation == Orientation.Horizontal)
                            port.Orientation = Orientation.Vertical;
                        else
                            port.Orientation = Orientation.Horizontal;
                        changedPorts.Add(port);
                    }
                }

                graphUpdater.MarkChanged(changedPorts);
            }
        }
    }
}
