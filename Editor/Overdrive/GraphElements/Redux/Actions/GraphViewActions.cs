using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class MoveElementsAction : ModelAction<IMovable, Vector2>
    {
        const string k_UndoStringSingular = "Move Element";
        const string k_UndoStringPlural = "Move Elements";

        public MoveElementsAction()
            : base(k_UndoStringSingular) {}

        public MoveElementsAction(Vector2 delta, IReadOnlyList<IMovable> models)
            : base(k_UndoStringSingular, k_UndoStringPlural, models, delta) {}

        public static void DefaultReducer(State state, MoveElementsAction action)
        {
            if (action.Models == null || action.Value == Vector2.zero)
                return;

            state.PushUndo(action);

            foreach (var placematModel in action.Models.OfType<IPlacematModel>())
            {
                placematModel.Move(action.Value);
            }

            // TODO It would be nice to have a single way of moving things around and thus not having to deal with 3
            // separate collections.
            foreach (var nodeModel in action.Models.OfType<INodeModel>())
            {
                nodeModel.Move(nodeModel.Position + action.Value);
            }

            foreach (var stickyNoteModel in action.Models.OfType<IStickyNoteModel>())
            {
                stickyNoteModel.PositionAndSize = new Rect(stickyNoteModel.PositionAndSize.position + action.Value, stickyNoteModel.PositionAndSize.size);
            }

            // Only move an edge if it is connected on both ends to a moving node.
            var edgeModels = action.Models.OfType<IEdgeModel>().ToList();
            if (edgeModels.Count > 0)
            {
                var nodeModels = new HashSet<INodeModel>(action.Models.OfType<INodeModel>());
                foreach (var edgeModel in edgeModels)
                {
                    if (edgeModel is IEditableEdge editableEdge &&
                        nodeModels.Contains(edgeModel.FromPort.NodeModel) &&
                        nodeModels.Contains(edgeModel.ToPort.NodeModel))
                    {
                        editableEdge.Move(action.Value);
                    }
                }
            }

            state.MarkChanged(action.Models.OfType<IGraphElementModel>());
        }
    }

    public class AutoPlaceElementsAction : ModelAction<IMovable>
    {
        const string k_UndoStringSingular = "Auto Place Element";
        const string k_UndoStringPlural = "Auto Place Elements";

        public IReadOnlyList<Vector2> Deltas;

        public AutoPlaceElementsAction()
            : base(k_UndoStringSingular) {}

        public AutoPlaceElementsAction(IReadOnlyList<Vector2> delta, IReadOnlyList<IMovable> models)
            : base(k_UndoStringSingular, k_UndoStringPlural, models)
        {
            Deltas = delta;
        }

        public static void DefaultReducer(State state, AutoPlaceElementsAction action)
        {
            if (action.Models == null || action.Deltas == null || action.Models.Count != action.Deltas.Count)
                return;

            state.PushUndo(action);

            for (int i = 0; i < action.Models.Count; ++i)
            {
                IMovable model = action.Models[i];
                Vector2 delta = action.Deltas[i];
                switch (model)
                {
                    case IPlacematModel _:
                        model.Move(delta);
                        break;
                    case INodeModel nodeModel:
                        nodeModel.Move(nodeModel.Position + delta);
                        break;
                    case IStickyNoteModel noteModel:
                        noteModel.PositionAndSize = new Rect(noteModel.PositionAndSize.position + delta, noteModel.PositionAndSize.size);
                        break;
                }
            }

            state.MarkChanged(action.Models.OfType<IGraphElementModel>());
        }
    }

    public class DeleteElementsAction : ModelAction<IGraphElementModel>
    {
        const string k_UndoStringSingular = "Delete Element";
        const string k_UndoStringPlural = "Delete Elements";

        public DeleteElementsAction()
            : base(k_UndoStringSingular) {}

        public DeleteElementsAction(IReadOnlyList<IGraphElementModel> elementsToRemove)
            : base(k_UndoStringSingular, k_UndoStringPlural, elementsToRemove)
        {
        }

        public static void DefaultReducer(State state, DeleteElementsAction action)
        {
            if (!action.Models.Any())
                return;

            state.PushUndo(action);

            var deletedModels = state.GraphModel.DeleteElements(action.Models);
            state.MarkDeleted(deletedModels);
        }
    }

    public class BuildAllEditorAction : BaseAction
    {
        public BuildAllEditorAction()
        {
            UndoString = "Compile Graph";
        }

        public static void DefaultReducer(State state, BuildAllEditorAction action)
        {
        }
    }

    public struct TargetInsertionInfo
    {
        public Vector2 Delta;
        public string OperationName;
    }

    public class PasteSerializedDataAction : BaseAction
    {
        public readonly TargetInsertionInfo Info;
        public readonly GtfoGraphView.CopyPasteData Data;

        public PasteSerializedDataAction()
        {
            UndoString = "Paste";
        }

        public PasteSerializedDataAction(TargetInsertionInfo info,
                                         GtfoGraphView.CopyPasteData data) : this()
        {
            Info = info;
            Data = data;
        }

        public static void DefaultReducer(State state, PasteSerializedDataAction action)
        {
            state.PushUndo(action);
            GtfoGraphView.PasteSerializedData(state.GraphModel, action.Info, state.SelectionStateComponent, action.Data);
            if (!action.Data.IsEmpty())
            {
                state.RequestUIRebuild();
            }
        }
    }
}
