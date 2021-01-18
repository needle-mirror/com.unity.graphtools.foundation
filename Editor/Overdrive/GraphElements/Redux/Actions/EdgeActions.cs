using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    static class EdgeActionConfig
    {
        public const int nodeOffset = 60;
    }

    public class CreateEdgeAction : ModelAction<IEdgeModel>
    {
        const string k_UndoString = "Create Edge";

        public IPortModel ToPortModel;
        public IPortModel FromPortModel;
        public Direction PortAlignment;
        public bool CreateItemizedNode;

        public CreateEdgeAction()
            : base(k_UndoString) {}

        public CreateEdgeAction(IPortModel toPortModel, IPortModel fromPortModel,
                                IReadOnlyList<IEdgeModel> edgeModelsToDelete = null,
                                Direction portAlignment = Direction.None,
                                bool createItemizedNode = true)
            : base(k_UndoString, k_UndoString, edgeModelsToDelete)
        {
            Assert.IsTrue(toPortModel == null || toPortModel.Direction == Direction.Input);
            Assert.IsTrue(fromPortModel == null || fromPortModel.Direction == Direction.Output);
            ToPortModel = toPortModel;
            FromPortModel = fromPortModel;
            PortAlignment = portAlignment;
            CreateItemizedNode = createItemizedNode;
        }

        public static void DefaultReducer(State state, CreateEdgeAction action)
        {
            state.PushUndo(action);

            var graphModel = state.GraphModel;

            if (action.Models != null)
            {
                graphModel.DeleteEdges(action.Models);
                state.MarkDeleted(action.Models);
            }

            var fromPortModel = action.FromPortModel;
            var toPortModel = action.ToPortModel;

            if (action.CreateItemizedNode)
            {
                graphModel.CreateItemizedNode(state, EdgeActionConfig.nodeOffset, ref fromPortModel);
                // We do not know what happened in CreateItemizedNode. Refresh the whole UI.
                state.RequestUIRebuild();
            }

            var edgeModel = graphModel.CreateEdge(toPortModel, fromPortModel);
            state.MarkNew(edgeModel);

            if (action.PortAlignment.HasFlag(Direction.Input))
                state.MarkModelToAutoAlign(toPortModel.NodeModel);
            if (action.PortAlignment.HasFlag(Direction.Output))
                state.MarkModelToAutoAlign(fromPortModel.NodeModel);
        }
    }

    public class DeleteEdgeAction : ModelAction<IEdgeModel>
    {
        const string k_UndoStringSingular = "Delete Edge";
        const string k_UndoStringPlural = "Delete Edges";

        public DeleteEdgeAction()
            : base(k_UndoStringSingular) {}

        public DeleteEdgeAction(IReadOnlyList<IEdgeModel> edgesToDelete)
            : base(k_UndoStringSingular, k_UndoStringPlural , edgesToDelete)
        {
        }

        public static void DefaultReducer(State state, DeleteEdgeAction action)
        {
            if (action.Models == null || !action.Models.Any())
                return;

            state.PushUndo(action);
            state.GraphModel.DeleteEdges(action.Models);
            state.MarkDeleted(action.Models);
        }
    }

    public class AddControlPointOnEdgeAction : BaseAction
    {
        public readonly IEditableEdge EdgeModel;
        public readonly int AtIndex;
        public readonly Vector2 Position;

        public AddControlPointOnEdgeAction()
        {
            UndoString = "Insert Control Point";
        }

        public AddControlPointOnEdgeAction(IEditableEdge edgeModel, int atIndex, Vector2 position) : this()
        {
            EdgeModel = edgeModel;
            AtIndex = atIndex;
            Position = position;
        }

        public static void DefaultReducer(State state, AddControlPointOnEdgeAction action)
        {
            state.PushUndo(action);
            action.EdgeModel.InsertEdgeControlPoint(action.AtIndex, action.Position, 100);
            state.MarkChanged(action.EdgeModel);
        }
    }

    public class MoveEdgeControlPointAction : BaseAction
    {
        public readonly IEditableEdge EdgeModel;
        public readonly int EdgeIndex;
        public readonly Vector2 NewPosition;
        public readonly float NewTightness;

        public MoveEdgeControlPointAction()
        {
            UndoString = "Edit Control Point";
        }

        public MoveEdgeControlPointAction(IEditableEdge edgeModel, int edgeIndex, Vector2 newPosition, float newTightness) : this()
        {
            EdgeModel = edgeModel;
            EdgeIndex = edgeIndex;
            NewPosition = newPosition;
            NewTightness = newTightness;
        }

        public static void DefaultReducer(State state, MoveEdgeControlPointAction action)
        {
            state.PushUndo(action);
            action.EdgeModel.ModifyEdgeControlPoint(action.EdgeIndex, action.NewPosition, action.NewTightness);
            state.MarkChanged(action.EdgeModel);
        }
    }

    public class RemoveEdgeControlPointAction : BaseAction
    {
        public readonly IEditableEdge EdgeModel;
        public readonly int EdgeIndex;

        public RemoveEdgeControlPointAction()
        {
            UndoString = "Remove Control Point";
        }

        public RemoveEdgeControlPointAction(IEditableEdge edgeModel, int edgeIndex) : this()
        {
            EdgeModel = edgeModel;
            EdgeIndex = edgeIndex;
        }

        public static void DefaultReducer(State state, RemoveEdgeControlPointAction action)
        {
            state.PushUndo(action);
            action.EdgeModel.RemoveEdgeControlPoint(action.EdgeIndex);
            state.MarkChanged(action.EdgeModel);
        }
    }

    public class SetEdgeEditModeAction : BaseAction
    {
        public readonly IEditableEdge EdgeModel;
        public readonly bool Value;

        public SetEdgeEditModeAction()
        {
            UndoString = "Set Edge Edit Mode";
        }

        public SetEdgeEditModeAction(IEditableEdge edgeModel, bool value) : this()
        {
            EdgeModel = edgeModel;
            Value = value;
        }

        public static void DefaultReducer(State state, SetEdgeEditModeAction action)
        {
            state.PushUndo(action);
            action.EdgeModel.EditMode = action.Value;
            state.MarkChanged(action.EdgeModel);
        }
    }

    public class ReorderEdgeAction : BaseAction
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

        public ReorderEdgeAction()
        {
            UndoString = "Reorder Edge";
        }

        public ReorderEdgeAction(IEdgeModel edgeModel, ReorderType type) : this()
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

        public static void DefaultReducer(State state, ReorderEdgeAction action)
        {
            if (action.EdgeModel?.FromPort is IReorderableEdgesPort fromPort && fromPort.HasReorderableEdges)
            {
                var siblingEdges = fromPort.GetConnectedEdges().ToList();
                var siblingEdgesCount = siblingEdges.Count;
                if (siblingEdgesCount > 1)
                {
                    var index = siblingEdges.IndexOf(action.EdgeModel);
                    Action<IEdgeModel> reorderAction = null;
                    switch (action.Type)
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
                        state.PushUndo(action);
                        reorderAction(action.EdgeModel);
                        fromPort.NodeModel.RevealReorderableEdgesOrder(true, action.EdgeModel);

                        state.MarkChanged(siblingEdges);
                        state.MarkChanged(fromPort.NodeModel);
                    }
                }
            }
        }
    }

    public class SplitEdgeAndInsertExistingNodeAction : BaseAction
    {
        public readonly IEdgeModel EdgeModel;
        public readonly IInOutPortsNode NodeModel;

        public SplitEdgeAndInsertExistingNodeAction()
        {
            UndoString = "Insert Node On Edge";
        }

        public SplitEdgeAndInsertExistingNodeAction(IEdgeModel edgeModel, IInOutPortsNode nodeModel) : this()
        {
            EdgeModel = edgeModel;
            NodeModel = nodeModel;
        }

        public static void DefaultReducer(State state, SplitEdgeAndInsertExistingNodeAction action)
        {
            Assert.IsTrue(action.NodeModel.InputsById.Count > 0);
            Assert.IsTrue(action.NodeModel.OutputsById.Count > 0);

            state.PushUndo(action);

            var graphModel = state.GraphModel;
            var edgeInput = action.EdgeModel.ToPort;
            var edgeOutput = action.EdgeModel.FromPort;
            var deletedModels = graphModel.DeleteEdge(action.EdgeModel);
            var edge1 = graphModel.CreateEdge(edgeInput, action.NodeModel.OutputsByDisplayOrder.First(p => p?.PortType == edgeInput?.PortType));
            var edge2 = graphModel.CreateEdge(action.NodeModel.InputsByDisplayOrder.First(p => p?.PortType == edgeOutput?.PortType), edgeOutput);

            state.MarkDeleted(deletedModels);
            state.MarkNew(edge1);
            state.MarkNew(edge2);
        }
    }

    public class ConvertEdgesToPortalsAction : BaseAction
    {
        const string k_UndoStringSingular = "Convert Edge to Portal";
        const string k_UndoStringPlural = "Convert Edges to Portals";

        static readonly Vector2 k_EntryPortalBaseOffset =  Vector2.right * 75;
        static readonly Vector2 k_ExitPortalBaseOffset = Vector2.left * 250;
        const int k_PortalHeight = 24;

        public (IEdgeModel edge, Vector2 startPortPos, Vector2 endPortPos)[] EdgeData;

        public ConvertEdgesToPortalsAction()
        {
            UndoString = k_UndoStringSingular;
        }

        public ConvertEdgesToPortalsAction(IReadOnlyList<(IEdgeModel, Vector2, Vector2)> edgeData) : this()
        {
            EdgeData = edgeData?.ToArray();
            UndoString = (EdgeData?.Length ?? 0) <= 1 ? k_UndoStringSingular : k_UndoStringPlural;
        }

        // TODO JOCE: Move to GraphView or something. We should be able to create from edge without a reducer (for tests, for example)
        public static void DefaultReducer(State state, ConvertEdgesToPortalsAction action)
        {
            var graphModel = state.GraphModel;

            if (action.EdgeData == null)
                return;

            var edgeData = action.EdgeData.ToList();
            if (!edgeData.Any())
                return;

            state.PushUndo(action);

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

            graphModel.DeleteEdges(edgeData.Select(d => d.edge).ToList());

            state.RequestUIRebuild();

            void ConvertEdgeToPortals((IEdgeModel edgeModel, Vector2 startPos, Vector2 endPos) data)
            {
                // Only a single portal per output port. Don't recreate if we already created one.
                var outputPortModel = data.edgeModel.FromPort;
                IEdgePortalEntryModel portalEntry = null;
                if (outputPortModel != null && !existingPortalEntries.TryGetValue(data.edgeModel.FromPort, out portalEntry))
                {
                    portalEntry = graphModel.CreateEntryPortalFromEdge(data.edgeModel);
                    existingPortalEntries[outputPortModel] = portalEntry;

                    if (!(outputPortModel.NodeModel is IInOutPortsNode nodeModel))
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

                    graphModel.CreateEdge(portalEntry.InputPort, outputPortModel);
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

                portalExit.Position = data.endPos + k_ExitPortalBaseOffset;
                {
                    if (data.edgeModel.ToPort.NodeModel is IInOutPortsNode nodeModel)
                    {
                        // y offset based on port order. hurgh.
                        var idx = nodeModel.InputsByDisplayOrder.IndexOf(inputPortModel);
                        portalExit.Position += Vector2.down * (k_PortalHeight * idx + 16); // Fudgy.
                    }
                }

                portalExit.DeclarationModel = portalEntry?.DeclarationModel;

                graphModel.CreateEdge(inputPortModel, portalExit.OutputPort);
            }
        }
    }

    public class TogglePortsAction : BaseAction
    {
        public TogglePortsAction()
        {
            UndoString = "Toggle Ports";
        }

        public static void DefaultReducer(State previousState, TogglePortsAction action)
        {
            previousState.PushUndo(action);

            var changedPorts = new List<IPortModel>();
            foreach (var node in previousState.GraphModel.NodeModels.OfType<IPortNode>())
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
            previousState.MarkChanged(changedPorts);
        }
    }

    public class ToggleEdgePortsAction : BaseAction
    {
        public readonly IEdgeModel[] EdgeModels;

        public ToggleEdgePortsAction(IEdgeModel[] edgeModels)
        {
            UndoString = "Toggle Edge Ports";
            EdgeModels = edgeModels;
        }

        public static void DefaultReducer(State previousState, ToggleEdgePortsAction action)
        {
            previousState.PushUndo(action);

            var changedPorts = new List<IPortModel>();

            foreach (var edgeModel in action.EdgeModels)
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
            previousState.MarkChanged(changedPorts);
        }
    }
}
