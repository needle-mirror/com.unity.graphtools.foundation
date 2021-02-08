using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class ResetElementColorCommand : Command
    {
        const string k_UndoStringSingular = "Reset Element Color";
        const string k_UndoStringPlural = "Reset Elements Color";

        public readonly IReadOnlyList<INodeModel> NodeModels;
        public readonly IReadOnlyList<IPlacematModel> PlacematModels;

        public ResetElementColorCommand()
        {
            UndoString = k_UndoStringSingular;
        }

        public ResetElementColorCommand(
            IReadOnlyList<INodeModel> nodeModels,
            IReadOnlyList<IPlacematModel> placematModels) : this()
        {
            NodeModels = nodeModels;
            PlacematModels = placematModels;

            UndoString = (NodeModels?.Count ?? 0) + (PlacematModels?.Count ?? 0) <= 1 ? k_UndoStringSingular : k_UndoStringPlural;
        }

        public static void DefaultCommandHandler(GraphToolState graphToolState, ResetElementColorCommand command)
        {
            graphToolState.PushUndo(command);

            if (command.NodeModels != null)
            {
                foreach (var model in command.NodeModels)
                {
                    model.HasUserColor = false;
                }
                graphToolState.MarkChanged(command.NodeModels);
            }
            if (command.PlacematModels != null)
            {
                foreach (var model in command.PlacematModels)
                {
                    model.ResetColor();
                }
                graphToolState.MarkChanged(command.PlacematModels);
            }
        }
    }

    public class ChangeElementColorCommand : Command
    {
        const string k_UndoStringSingular = "Change Element Color";
        const string k_UndoStringPlural = "Change Elements Color";

        public readonly IReadOnlyList<INodeModel> NodeModels;
        public readonly IReadOnlyList<IPlacematModel> PlacematModels;
        public readonly Color Color;

        public ChangeElementColorCommand()
        {
            UndoString = k_UndoStringSingular;
        }

        public ChangeElementColorCommand(Color color,
                                         IReadOnlyList<INodeModel> nodeModels,
                                         IReadOnlyList<IPlacematModel> placematModels) : this()
        {
            NodeModels = nodeModels;
            PlacematModels = placematModels;
            Color = color;

            UndoString = (NodeModels?.Count ?? 0) + (PlacematModels?.Count ?? 0) <= 1 ? k_UndoStringSingular : k_UndoStringPlural;
        }

        public static void DefaultCommandHandler(GraphToolState graphToolState, ChangeElementColorCommand command)
        {
            graphToolState.PushUndo(command);

            if (command.NodeModels != null)
            {
                foreach (var model in command.NodeModels)
                {
                    model.Color = command.Color;
                }
                graphToolState.MarkChanged(command.NodeModels);
            }

            if (command.PlacematModels != null)
            {
                foreach (var model in command.PlacematModels)
                {
                    model.Color = command.Color;
                }
                graphToolState.MarkChanged(command.PlacematModels);
            }
        }
    }

    public class AlignNodesCommand : Command
    {
        public readonly GraphView GraphView;
        public readonly bool Follow;

        public AlignNodesCommand()
        {
            UndoString = "Align Items";
        }

        public AlignNodesCommand(GraphView graphView, bool follow) : this()
        {
            GraphView = graphView;
            Follow = follow;

            if (follow)
            {
                UndoString = "Align Hierarchies";
            }
        }

        public static void DefaultCommandHandler(GraphToolState graphToolState, AlignNodesCommand command)
        {
            graphToolState.PushUndo(command);
            command.GraphView.PositionDependenciesManager.AlignNodes(command.Follow,
                command.GraphView.Selection.OfType<IModelUI>().Select(ge => ge.Model).ToList());
            graphToolState.RequestUIRebuild();
        }
    }
}
