using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class ResetElementColorAction : BaseAction
    {
        const string k_UndoStringSingular = "Reset Element Color";
        const string k_UndoStringPlural = "Reset Elements Color";

        public readonly IReadOnlyList<INodeModel> NodeModels;
        public readonly IReadOnlyList<IPlacematModel> PlacematModels;

        public ResetElementColorAction()
        {
            UndoString = k_UndoStringSingular;
        }

        public ResetElementColorAction(
            IReadOnlyList<INodeModel> nodeModels,
            IReadOnlyList<IPlacematModel> placematModels) : this()
        {
            NodeModels = nodeModels;
            PlacematModels = placematModels;

            UndoString = (NodeModels?.Count ?? 0) + (PlacematModels?.Count ?? 0) <= 1 ? k_UndoStringSingular : k_UndoStringPlural;
        }

        public static void DefaultReducer(State previousState, ResetElementColorAction action)
        {
            previousState.PushUndo(action);

            if (action.NodeModels != null)
                foreach (var model in action.NodeModels)
                {
                    model.HasUserColor = false;
                    previousState.MarkForUpdate(UpdateFlags.UpdateView, model);
                }
            if (action.PlacematModels != null)
                foreach (var model in action.PlacematModels)
                {
                    model.ResetColor();
                    previousState.MarkForUpdate(UpdateFlags.UpdateView, model);
                }
        }
    }

    public class ChangeElementColorAction : BaseAction
    {
        const string k_UndoStringSingular = "Change Element Color";
        const string k_UndoStringPlural = "Change Elements Color";

        public readonly IReadOnlyList<INodeModel> NodeModels;
        public readonly IReadOnlyList<IPlacematModel> PlacematModels;
        public readonly Color Color;

        public ChangeElementColorAction()
        {
            UndoString = k_UndoStringSingular;
        }

        public ChangeElementColorAction(Color color,
                                        IReadOnlyList<INodeModel> nodeModels,
                                        IReadOnlyList<IPlacematModel> placematModels) : this()
        {
            NodeModels = nodeModels;
            PlacematModels = placematModels;
            Color = color;

            UndoString = (NodeModels?.Count ?? 0) + (PlacematModels?.Count ?? 0) <= 1 ? k_UndoStringSingular : k_UndoStringPlural;
        }

        public static void DefaultReducer(State previousState, ChangeElementColorAction action)
        {
            previousState.PushUndo(action);

            if (action.NodeModels != null)
                foreach (var model in action.NodeModels)
                {
                    model.Color = action.Color;
                    previousState.MarkForUpdate(UpdateFlags.UpdateView, model);
                }

            if (action.PlacematModels != null)
                foreach (var model in action.PlacematModels)
                {
                    model.Color = action.Color;
                    previousState.MarkForUpdate(UpdateFlags.UpdateView, model);
                }
        }
    }

    public class AlignNodesAction : BaseAction
    {
        public readonly GraphView GraphView;
        public readonly bool Follow;

        public AlignNodesAction()
        {
            UndoString = "Align Items";
        }

        public AlignNodesAction(GraphView graphView, bool follow) : this()
        {
            GraphView = graphView;
            Follow = follow;
        }

        public static void DefaultReducer(State previousState, AlignNodesAction action)
        {
            previousState.PushUndo(action);
            action.GraphView.PositionDependenciesManagers.AlignNodes(action.GraphView, action.Follow, action.GraphView.Selection);
        }
    }
}
