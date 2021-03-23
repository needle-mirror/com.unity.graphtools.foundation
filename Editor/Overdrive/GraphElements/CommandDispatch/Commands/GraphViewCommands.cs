using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class MoveElementsCommand : ModelCommand<IMovable, Vector2>
    {
        const string k_UndoStringSingular = "Move Element";
        const string k_UndoStringPlural = "Move Elements";

        public MoveElementsCommand()
            : base(k_UndoStringSingular) { }

        public MoveElementsCommand(Vector2 delta, IReadOnlyList<IMovable> models)
            : base(k_UndoStringSingular, k_UndoStringPlural, models, delta) { }

        public static void DefaultCommandHandler(GraphToolState graphToolState, MoveElementsCommand command)
        {
            if (command.Models == null || command.Value == Vector2.zero)
                return;

            graphToolState.PushUndo(command);

            using (var graphUpdater = graphToolState.GraphViewState.Updater)
            {
                var movingNodes = command.Models.OfType<INodeModel>().ToList();

                foreach (var movable in command.Models

                    // Only move an edge if it is connected on both ends to a moving node.
                    .Where(m => !(m is IEditableEdge e)
                        || movingNodes.Contains(e.FromPort.NodeModel)
                        && movingNodes.Contains(e.ToPort.NodeModel)))
                {
                    movable.Move(command.Value);
                }

                graphUpdater.U.MarkChanged(command.Models.OfType<IGraphElementModel>());
            }
        }
    }

    public class AutoPlaceElementsCommand : ModelCommand<IMovable>
    {
        const string k_UndoStringSingular = "Auto Place Element";
        const string k_UndoStringPlural = "Auto Place Elements";

        public IReadOnlyList<Vector2> Deltas;

        public AutoPlaceElementsCommand()
            : base(k_UndoStringSingular) { }

        public AutoPlaceElementsCommand(IReadOnlyList<Vector2> delta, IReadOnlyList<IMovable> models)
            : base(k_UndoStringSingular, k_UndoStringPlural, models)
        {
            Deltas = delta;
        }

        public static void DefaultCommandHandler(GraphToolState graphToolState, AutoPlaceElementsCommand command)
        {
            if (command.Models == null || command.Deltas == null || command.Models.Count != command.Deltas.Count)
                return;

            graphToolState.PushUndo(command);

            using (var graphUpdater = graphToolState.GraphViewState.Updater)
            {
                for (int i = 0; i < command.Models.Count; ++i)
                {
                    IMovable model = command.Models[i];
                    Vector2 delta = command.Deltas[i];
                    model.Move(delta);
                }

                graphUpdater.U.MarkChanged(command.Models.OfType<IGraphElementModel>());
            }
        }
    }

    public class DeleteElementsCommand : ModelCommand<IGraphElementModel>
    {
        const string k_UndoStringSingular = "Delete Element";
        const string k_UndoStringPlural = "Delete Elements";

        public DeleteElementsCommand()
            : base(k_UndoStringSingular) { }

        public DeleteElementsCommand(IReadOnlyList<IGraphElementModel> elementsToRemove)
            : base(k_UndoStringSingular, k_UndoStringPlural, elementsToRemove)
        {
        }

        public static void DefaultCommandHandler(GraphToolState graphToolState, DeleteElementsCommand command)
        {
            if (!command.Models.Any())
                return;

            graphToolState.PushUndo(command);

            using (var selectionUpdater = graphToolState.SelectionState.Updater)
            using (var graphUpdater = graphToolState.GraphViewState.Updater)
            {
                var deletedModels = graphToolState.GraphViewState.GraphModel.DeleteElements(command.Models).ToList();

                var selectedModels = deletedModels.Where(m => graphToolState.SelectionState.IsSelected(m)).ToList();
                if (selectedModels.Any())
                {
                    selectionUpdater.U.SelectElements(selectedModels, false);
                }

                graphUpdater.U.MarkDeleted(deletedModels);
            }
        }
    }

    public class BuildAllEditorCommand : Command
    {
        public BuildAllEditorCommand()
        {
            UndoString = "Compile Graph";
        }

        public static void DefaultCommandHandler(GraphToolState graphToolState, BuildAllEditorCommand command)
        {
        }
    }

    public struct TargetInsertionInfo
    {
        public Vector2 Delta;
        public string OperationName;
    }

    public class PasteSerializedDataCommand : Command
    {
        public readonly TargetInsertionInfo Info;
        public readonly CopyPasteData Data;

        public PasteSerializedDataCommand()
        {
            UndoString = "Paste";
        }

        public PasteSerializedDataCommand(TargetInsertionInfo info,
                                          CopyPasteData data) : this()
        {
            Info = info;
            Data = data;
        }

        public static void DefaultCommandHandler(GraphToolState graphToolState, PasteSerializedDataCommand command)
        {
            if (!command.Data.IsEmpty())
            {
                graphToolState.PushUndo(command);

                using (var graphViewUpdater = graphToolState.GraphViewState.Updater)
                using (var selectionUpdater = graphToolState.SelectionState.Updater)
                {
                    selectionUpdater.U.ClearSelection(graphToolState.GraphViewState.GraphModel);

                    CopyPasteData.PasteSerializedData(graphToolState.GraphViewState.GraphModel, command.Info, graphViewUpdater.U, selectionUpdater.U, command.Data);
                }
            }
        }
    }

    /// <summary>
    /// A command to change the graph view position and zoom and optionally change the selection.
    /// </summary>
    public class ReframeGraphViewCommand : Command
    {
        /// <summary>
        /// The new position.
        /// </summary>
        public Vector3 Position;
        /// <summary>
        /// The new zoom factor.
        /// </summary>
        public Vector3 Scale;
        /// <summary>
        /// The elements to select, in replacement of the current selection.
        /// </summary>
        public List<IGraphElementModel> NewSelection;

        /// <summary>
        /// Initializes a new instance of the ReframeGraphViewCommand class.
        /// </summary>
        public ReframeGraphViewCommand()
        {
            UndoString = "Reframe View";
        }

        /// <summary>
        /// Initializes a new instance of the ReframeGraphViewCommand class.
        /// </summary>
        /// <param name="position">The new position.</param>
        /// <param name="scale">The new zoom factor.</param>
        /// <param name="newSelection">If not null, the elements to select, in replacement of the current selection.
        /// If null, the selection is not changed.</param>
        public ReframeGraphViewCommand(Vector3 position, Vector3 scale, List<IGraphElementModel> newSelection = null) : this()
        {
            Position = position;
            Scale = scale;
            NewSelection = newSelection;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="state">The state to modify.</param>
        /// <param name="command">The command to apply to the state.</param>
        public static void DefaultCommandHandler(GraphToolState state, ReframeGraphViewCommand command)
        {
            state.PushUndo(command);

            using (var selectionUpdater = state.SelectionState.Updater)
            using (var graphUpdater = state.GraphViewState.Updater)
            {
                graphUpdater.U.Position = command.Position;
                graphUpdater.U.Scale = command.Scale;

                if (command.NewSelection != null)
                {
                    selectionUpdater.U.ClearSelection(state.GraphViewState.GraphModel);
                    selectionUpdater.U.SelectElements(command.NewSelection, true);
                }
            }
        }
    }
}
