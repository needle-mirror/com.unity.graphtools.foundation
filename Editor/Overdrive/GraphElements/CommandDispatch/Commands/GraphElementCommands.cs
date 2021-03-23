using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A command to reset the color of some graph element models
    /// </summary>
    public class ResetElementColorCommand : ModelCommand<IGraphElementModel>
    {
        const string k_UndoStringSingular = "Reset Element Color";
        const string k_UndoStringPlural = "Reset Elements Color";

        /// <summary>
        /// Initializes a new instance of the ResetElementColorCommand class.
        /// </summary>
        public ResetElementColorCommand()
            : base(k_UndoStringSingular)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ResetElementColorCommand class.
        /// </summary>
        /// <param name="models">Element models to reset</param>
        public ResetElementColorCommand(IReadOnlyList<IGraphElementModel> models)
            : base(k_UndoStringSingular, k_UndoStringPlural, models)
        {
        }

        /// <summary>
        /// Default command handler
        /// </summary>
        /// <param name="graphToolState">The state to modify.</param>
        /// <param name="command">The command to apply to the state.</param>
        public static void DefaultCommandHandler(GraphToolState graphToolState, ResetElementColorCommand command)
        {
            graphToolState.PushUndo(command);

            if (command.Models != null)
            {
                using (var updater = graphToolState.GraphViewState.Updater)
                {
                    foreach (var model in command.Models.Where(c => c.IsColorable()))
                    {
                        model.ResetColor();
                    }

                    updater.U.MarkChanged(command.Models);
                }
            }
        }
    }

    /// <summary>
    /// A command to change the color of some graph element models
    /// </summary>
    public class ChangeElementColorCommand : ModelCommand<IGraphElementModel>
    {
        const string k_UndoStringSingular = "Change Element Color";
        const string k_UndoStringPlural = "Change Elements Color";
        public readonly Color Color;

        /// <summary>
        /// Initializes a new instance of the ChangeElementColorCommand class.
        /// </summary>
        public ChangeElementColorCommand()
            : base(k_UndoStringSingular)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ChangeElementColorCommand class.
        /// </summary>
        /// <param name="color">the color to set</param>
        /// <param name="elementModels">Element models to affect</param>
        public ChangeElementColorCommand(Color color, IReadOnlyList<IGraphElementModel> elementModels)
            : base(k_UndoStringSingular, k_UndoStringPlural, elementModels)
        {
            Color = color;
        }

        /// <summary>
        /// Default command handler
        /// </summary>
        /// <param name="graphToolState">The state to modify.</param>
        /// <param name="command">The command to apply to the state.</param>
        public static void DefaultCommandHandler(GraphToolState graphToolState, ChangeElementColorCommand command)
        {
            graphToolState.PushUndo(command);

            if (command.Models != null)
            {
                using (var updater = graphToolState.GraphViewState.Updater)
                {
                    foreach (var model in command.Models.Where(c => c.IsColorable()))
                    {
                        model.Color = command.Color;
                    }

                    updater.U.MarkChanged(command.Models);
                }
            }
        }
    }

    /// <summary>
    /// Command to align nodes hierarchies in a graph view.
    /// </summary>
    public class AlignNodesCommand : Command
    {
        /// <summary>
        /// The GraphView in charge of aligning the nodes.
        /// </summary>
        public readonly GraphView GraphView;
        /// <summary>
        /// A list of nodes to align.
        /// </summary>
        public readonly IReadOnlyList<IGraphElementModel> Nodes;
        /// <summary>
        /// True if hierarchies should be aligned. Otherwise, only the nodes in <cref name="Nodes"/> are aligned.
        /// </summary>
        public readonly bool Follow;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public AlignNodesCommand()
        {
            UndoString = "Align Items";
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="graphView">The GraphView in charge of aligning the nodes.</param>
        /// <param name="nodes">A list of nodes to align.</param>
        /// <param name="follow">True if hierarchies should be aligned. Otherwise, only the nodes in <paramref name="nodes"/> are aligned.</param>
        public AlignNodesCommand(GraphView graphView, IReadOnlyList<IGraphElementModel> nodes, bool follow) : this()
        {
            GraphView = graphView;
            Nodes = nodes;
            Follow = follow;

            if (follow)
            {
                UndoString = "Align Hierarchies";
            }
        }

        /// <summary>
        /// Default handler.
        /// </summary>
        /// <param name="graphToolState">The state to modify.</param>
        /// <param name="command">The command to apply to the state.</param>
        public static void DefaultCommandHandler(GraphToolState graphToolState, AlignNodesCommand command)
        {
            if (command.Nodes.Any())
            {
                graphToolState.PushUndo(command);
                using (var stateUpdater = graphToolState.GraphViewState.Updater)
                {
                    command.GraphView.PositionDependenciesManager.AlignNodes(command.Follow, command.Nodes);
                    stateUpdater.U.ForceCompleteUpdate();
                }
            }
        }
    }

    /// <summary>
    /// A command to select a graph element models.
    /// </summary>
    public class SelectElementsCommand : ModelCommand<IGraphElementModel>
    {
        /// <summary>
        /// Selection mode.
        /// </summary>
        public enum SelectionMode
        {
            /// <summary>
            /// Replace the selection.
            /// </summary>
            Replace,
            /// <summary>
            /// Add to the selection.
            /// </summary>
            Add,
            /// <summary>
            /// Remove from the selection.
            /// </summary>
            Remove,
            /// <summary>
            /// If the element is not currently selected,
            /// add it to the selection. Otherwise remove it from the selection.
            /// </summary>
            Toggle,
        }

        const string k_UndoStringSingular = "Select Element";
        const string k_UndoStringPlural = "Select Elements";

        /// <summary>
        /// The selection mode.
        /// </summary>
        public SelectionMode Mode;

        /// <summary>
        /// Initializes a new instance of the SelectElementsCommand class.
        /// </summary>
        public SelectElementsCommand()
            : base(k_UndoStringSingular) { }

        /// <summary>
        /// Initializes a new instance of the SelectElementsCommand class.
        /// </summary>
        /// <param name="mode">How should the selection should be modified.</param>
        /// <param name="models">The list of models affected by this command.</param>
        public SelectElementsCommand(SelectionMode mode, IReadOnlyList<IGraphElementModel> models)
            : base(k_UndoStringSingular, k_UndoStringPlural, models)
        {
            Mode = mode;
        }

        /// <summary>
        /// Initializes a new instance of the SelectElementsCommand class.
        /// </summary>
        /// <param name="mode">How should the selection should be modified.</param>
        /// <param name="models">The list of models affected by this command.</param>
        public SelectElementsCommand(SelectionMode mode, params IGraphElementModel[] models)
            : base(k_UndoStringSingular, k_UndoStringPlural, models)
        {
            Mode = mode;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="state">The state to modify.</param>
        /// <param name="command">The command to apply to the state.</param>
        public static void DefaultCommandHandler(GraphToolState state, SelectElementsCommand command)
        {
            switch (command.Mode)
            {
                case SelectionMode.Replace:
                    var currentSelection = state.SelectionState.GetSelection(state.GraphViewState.GraphModel);
                    if (currentSelection.SequenceEqual(command.Models))
                        return;
                    break;
                case SelectionMode.Add when command.Models.Count == 0:
                case SelectionMode.Remove when command.Models.Count == 0:
                case SelectionMode.Toggle when command.Models.Count == 0:
                    return;
            }

            state.PushUndo(command);

            using (var selectionUpdater = state.SelectionState.Updater)
            {
                switch (command.Mode)
                {
                    case SelectionMode.Replace:
                        selectionUpdater.U.ClearSelection(state.GraphViewState.GraphModel);
                        selectionUpdater.U.SelectElements(command.Models, true);
                        break;
                    case SelectionMode.Add:
                        selectionUpdater.U.SelectElements(command.Models, true);
                        break;
                    case SelectionMode.Remove:
                        selectionUpdater.U.SelectElements(command.Models, false);
                        break;
                    case SelectionMode.Toggle:
                        var toSelect = command.Models.Where(m => !state.SelectionState.IsSelected(m)).ToList();
                        selectionUpdater.U.SelectElements(command.Models, false);
                        selectionUpdater.U.SelectElements(toSelect, true);
                        break;
                }
            }
        }
    }

    /// <summary>
    /// A command to clear the selection.
    /// </summary>
    public class ClearSelectionCommand : Command
    {
        const string k_UndoStringSingular = "Clear Selection";

        /// <summary>
        /// Initializes a new instance of the ClearSelectionCommand class.
        /// </summary>
        public ClearSelectionCommand()
        {
            UndoString = k_UndoStringSingular;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="state">The state to modify.</param>
        /// <param name="command">The command to apply to the state.</param>
        public static void DefaultCommandHandler(GraphToolState state, ClearSelectionCommand command)
        {
            var currentSelection = state.SelectionState.GetSelection(state.GraphViewState.GraphModel);
            if (currentSelection.Count == 0)
                return;

            state.PushUndo(command);

            using (var selectionUpdater = state.SelectionState.Updater)
            {
                selectionUpdater.U.ClearSelection(state.GraphViewState.GraphModel);
            }
        }
    }

    /// <summary>
    /// A command to change the position and size of an element.
    /// </summary>
    public class ChangeElementLayoutCommand : Command
    {
        const string k_UndoStringSingular = "Resize Element";

        /// <summary>
        /// The model to resize.
        /// </summary>
        public IResizable Model;
        /// <summary>
        /// The new layout.
        /// </summary>
        public Rect Layout;

        /// <summary>
        /// Initializes a new instance of the ChangeElementLayoutCommand class.
        /// </summary>
        public ChangeElementLayoutCommand()
        {
            UndoString = k_UndoStringSingular;
        }

        /// <summary>
        /// Initializes a new instance of the ChangeElementLayoutCommand class.
        /// </summary>
        /// <param name="resizableModel">The model to resize.</param>
        /// <param name="newLayout">The new position and size.</param>
        public ChangeElementLayoutCommand(IResizable resizableModel, Rect newLayout)
            : this()
        {
            Model = resizableModel;
            Layout = newLayout;
        }

        /// <summary>
        /// Default command handler for ChangeElementLayoutCommand.
        /// </summary>
        /// <param name="graphToolState">The state.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(GraphToolState graphToolState, ChangeElementLayoutCommand command)
        {
            if (command.Model.PositionAndSize == command.Layout)
                return;

            graphToolState.PushUndo(command);

            using (var graphUpdater = graphToolState.GraphViewState.Updater)
            {
                command.Model.PositionAndSize = command.Layout;
                graphUpdater.U.MarkChanged(command.Model as IGraphElementModel);
            }
        }
    }
}
