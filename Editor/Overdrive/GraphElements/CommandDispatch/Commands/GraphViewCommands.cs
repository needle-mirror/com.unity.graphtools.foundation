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
            : base(k_UndoStringSingular) {}

        public MoveElementsCommand(Vector2 delta, IReadOnlyList<IMovable> models)
            : base(k_UndoStringSingular, k_UndoStringPlural, models, delta) {}

        public static void DefaultCommandHandler(GraphToolState graphToolState, MoveElementsCommand command)
        {
            if (command.Models == null || command.Value == Vector2.zero)
                return;

            graphToolState.PushUndo(command);

            var movingNodes = command.Models.OfType<INodeModel>().ToList();

            foreach (var movable in command.Models
                     // Only move an edge if it is connected on both ends to a moving node.
                     .Where(m => !(m is IEditableEdge e)
                         || movingNodes.Contains(e.FromPort.NodeModel)
                         && movingNodes.Contains(e.ToPort.NodeModel)))
            {
                movable.Move(command.Value);
            }

            graphToolState.MarkChanged(command.Models.OfType<IGraphElementModel>());
        }
    }

    public class AutoPlaceElementsCommand : ModelCommand<IMovable>
    {
        const string k_UndoStringSingular = "Auto Place Element";
        const string k_UndoStringPlural = "Auto Place Elements";

        public IReadOnlyList<Vector2> Deltas;

        public AutoPlaceElementsCommand()
            : base(k_UndoStringSingular) {}

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

            for (int i = 0; i < command.Models.Count; ++i)
            {
                IMovable model = command.Models[i];
                Vector2 delta = command.Deltas[i];
                model.Move(delta);
            }

            graphToolState.MarkChanged(command.Models.OfType<IGraphElementModel>());
        }
    }

    public class DeleteElementsCommand : ModelCommand<IGraphElementModel>
    {
        const string k_UndoStringSingular = "Delete Element";
        const string k_UndoStringPlural = "Delete Elements";

        public DeleteElementsCommand()
            : base(k_UndoStringSingular) {}

        public DeleteElementsCommand(IReadOnlyList<IGraphElementModel> elementsToRemove)
            : base(k_UndoStringSingular, k_UndoStringPlural, elementsToRemove)
        {
        }

        public static void DefaultCommandHandler(GraphToolState graphToolState, DeleteElementsCommand command)
        {
            if (!command.Models.Any())
                return;

            graphToolState.PushUndo(command);

            var deletedModels = graphToolState.GraphModel.DeleteElements(command.Models);
            graphToolState.MarkDeleted(deletedModels);
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
            graphToolState.PushUndo(command);
            CopyPasteData.PasteSerializedData(graphToolState.GraphModel, command.Info, graphToolState.SelectionStateComponent, command.Data);
            if (!command.Data.IsEmpty())
            {
                graphToolState.RequestUIRebuild();
            }
        }
    }
}
