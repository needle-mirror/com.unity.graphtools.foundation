using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    public class CreateEdgeAction : IAction
    {
        [Flags]
        public enum PortAlignmentType
        {
            None = 0,
            Input = 1,
            Output = 2,
        }

        public IGTFPortModel InputPortModel;
        public IGTFPortModel OutputPortModel;
        public IGTFEdgeModel[] EdgeModelsToDelete;
        public PortAlignmentType PortAlignment;

        public CreateEdgeAction()
        {
        }

        public CreateEdgeAction(IGTFPortModel inputPortModel, IGTFPortModel outputPortModel,
                                IEnumerable<IGTFEdgeModel> edgeModelsToDelete = null, PortAlignmentType portAlignment = PortAlignmentType.None)
        {
            Assert.IsTrue(inputPortModel.Direction == Direction.Input);
            Assert.IsTrue(outputPortModel.Direction == Direction.Output);
            InputPortModel = inputPortModel;
            OutputPortModel = outputPortModel;
            EdgeModelsToDelete = edgeModelsToDelete?.ToArray();
            PortAlignment = portAlignment;
        }

        public static State DefaultReducer(State previousState, CreateEdgeAction action)
        {
            var graphModel = previousState.CurrentGraphModel;

            if (action.EdgeModelsToDelete != null)
                graphModel.DeleteElements(action.EdgeModelsToDelete);

            IGTFPortModel outputPortModel = action.OutputPortModel;
            IGTFPortModel inputPortModel = action.InputPortModel;

            graphModel.CreateEdge(inputPortModel, outputPortModel);

            return previousState;
        }
    }

    public class AddControlPointOnEdgeAction : IAction
    {
        public readonly IEditableEdge EdgeModel;
        public readonly int AtIndex;
        public readonly Vector2 Position;

        public AddControlPointOnEdgeAction(IEditableEdge edgeModel, int atIndex, Vector2 position)
        {
            EdgeModel = edgeModel;
            AtIndex = atIndex;
            Position = position;
        }

        public static TState DefaultReducer<TState>(TState previousState, AddControlPointOnEdgeAction action) where TState : State
        {
            var graphModel = previousState.AssetModel;
            Undo.RegisterCompleteObjectUndo(graphModel as Object, "Insert Control Point");
            action.EdgeModel.InsertEdgeControlPoint(action.AtIndex, action.Position, 100);
            previousState.MarkForUpdate(UpdateFlags.UpdateView, action.EdgeModel);
            return previousState;
        }
    }

    public class MoveEdgeControlPointAction : IAction
    {
        public readonly IEditableEdge EdgeModel;
        public readonly int EdgeIndex;
        public readonly Vector2 NewPosition;
        public readonly float NewTightness;

        public MoveEdgeControlPointAction(IEditableEdge edgeModel, int edgeIndex, Vector2 newPosition, float newTightness)
        {
            EdgeModel = edgeModel;
            EdgeIndex = edgeIndex;
            NewPosition = newPosition;
            NewTightness = newTightness;
        }

        public static TState DefaultReducer<TState>(TState previousState, MoveEdgeControlPointAction action) where TState : State
        {
            var graphModel = previousState.AssetModel;
            Undo.RegisterCompleteObjectUndo(graphModel as Object, "Edit Control Point");
            action.EdgeModel.ModifyEdgeControlPoint(action.EdgeIndex, action.NewPosition, action.NewTightness);
            return previousState;
        }
    }

    public class RemoveEdgeControlPointAction : IAction
    {
        public readonly IEditableEdge EdgeModel;
        public readonly int EdgeIndex;

        public RemoveEdgeControlPointAction(IEditableEdge edgeModel, int edgeIndex)
        {
            EdgeModel = edgeModel;
            EdgeIndex = edgeIndex;
        }

        public static TState DefaultReducer<TState>(TState previousState, RemoveEdgeControlPointAction action) where TState : State
        {
            var graphModel = previousState.AssetModel;
            Undo.RegisterCompleteObjectUndo(graphModel as Object, "Remove Control Point");
            action.EdgeModel.RemoveEdgeControlPoint(action.EdgeIndex);
            previousState.MarkForUpdate(UpdateFlags.UpdateView, action.EdgeModel);
            return previousState;
        }
    }

    public class SetEdgeEditModeAction : IAction
    {
        public readonly IEditableEdge EdgeModel;
        public readonly bool Value;

        public SetEdgeEditModeAction(IEditableEdge edgeModel, bool value)
        {
            EdgeModel = edgeModel;
            Value = value;
        }

        public static TState DefaultReducer<TState>(TState previousState, SetEdgeEditModeAction action) where TState : State
        {
            action.EdgeModel.EditMode = action.Value;
            previousState.MarkForUpdate(UpdateFlags.UpdateView, action.EdgeModel);
            return previousState;
        }
    }

    public class ReorderEdgeAction : IAction
    {
        public enum ReorderType
        {
            MoveFirst,
            MoveUp,
            MoveDown,
            MoveLast
        }

        public readonly IGTFEdgeModel EdgeModel;
        public readonly ReorderType Type;

        public ReorderEdgeAction(IGTFEdgeModel edgeModel, ReorderType type)
        {
            EdgeModel = edgeModel;
            Type = type;
        }

        public static TState DefaultReducer<TState>(TState previousState, ReorderEdgeAction action) where TState : State
        {
            if (action.EdgeModel?.FromPort is IReorderableEdgesPort fromPort && fromPort.HasReorderableEdges)
            {
                var siblingEdges = fromPort.GetConnectedEdges().ToList();
                var siblingEdgesCount = siblingEdges.Count;
                if (siblingEdgesCount > 1)
                {
                    var index = siblingEdges.IndexOf(action.EdgeModel);
                    Action<IGTFEdgeModel> reorderAction = null;
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
                        var graphModel = previousState.AssetModel;
                        Undo.RegisterCompleteObjectUndo(graphModel as Object, "Reorder Edge : " + action.Type);
                        reorderAction(action.EdgeModel);
                        previousState.MarkForUpdate(UpdateFlags.RequestCompilation, action.EdgeModel);
                    }
                }
            }

            return previousState;
        }
    }
}
