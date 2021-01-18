using System;
using System.Collections.Generic;
using System.Linq;
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

        public static void DefaultReducer(State state, ResetElementColorAction action)
        {
            state.PushUndo(action);

            if (action.NodeModels != null)
            {
                foreach (var model in action.NodeModels)
                {
                    model.HasUserColor = false;
                }
                state.MarkChanged(action.NodeModels);
            }
            if (action.PlacematModels != null)
            {
                foreach (var model in action.PlacematModels)
                {
                    model.ResetColor();
                }
                state.MarkChanged(action.PlacematModels);
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

        public static void DefaultReducer(State state, ChangeElementColorAction action)
        {
            state.PushUndo(action);

            if (action.NodeModels != null)
            {
                foreach (var model in action.NodeModels)
                {
                    model.Color = action.Color;
                }
                state.MarkChanged(action.NodeModels);
            }

            if (action.PlacematModels != null)
            {
                foreach (var model in action.PlacematModels)
                {
                    model.Color = action.Color;
                }
                state.MarkChanged(action.PlacematModels);
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

            if (follow)
            {
                UndoString = "Align Hierarchies";
            }
        }

        public static void DefaultReducer(State state, AlignNodesAction action)
        {
            state.PushUndo(action);
            action.GraphView.PositionDependenciesManager.AlignNodes(action.Follow,
                action.GraphView.Selection.OfType<IGraphElement>().Select(ge => ge.Model).ToList());
            state.RequestUIRebuild();
        }
    }
}
