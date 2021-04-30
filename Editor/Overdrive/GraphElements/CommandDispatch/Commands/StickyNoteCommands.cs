using System.Linq;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class CreateStickyNoteCommand : UndoableCommand
    {
        public readonly Rect Position;

        public CreateStickyNoteCommand()
        {
            UndoString = "Create Sticky Note";
        }

        public CreateStickyNoteCommand(Rect position) : this()
        {
            Position = position;
        }

        public static void DefaultCommandHandler(GraphToolState graphToolState, CreateStickyNoteCommand command)
        {
            graphToolState.PushUndo(command);

            using (var graphUpdater = graphToolState.GraphViewState.UpdateScope)
            {
                var stickyNote = graphToolState.GraphViewState.GraphModel.CreateStickyNote(command.Position);
                graphUpdater.MarkNew(stickyNote);
            }
        }
    }

    public class UpdateStickyNoteCommand : UndoableCommand
    {
        public readonly string Title;
        public readonly string Contents;
        public readonly IStickyNoteModel StickyNoteModel;

        public UpdateStickyNoteCommand()
        {
            UndoString = "Update Sticky Note Content";
        }

        public UpdateStickyNoteCommand(IStickyNoteModel stickyNoteModel, string title, string contents) : this()
        {
            StickyNoteModel = stickyNoteModel;
            Title = title;
            Contents = contents;
        }

        public static void DefaultCommandHandler(GraphToolState graphToolState, UpdateStickyNoteCommand command)
        {
            if (command.Title == null && command.Contents == null)
                return;

            graphToolState.PushUndo(command);

            using (var graphUpdater = graphToolState.GraphViewState.UpdateScope)
            {
                if (command.Title != null)
                    command.StickyNoteModel.Title = command.Title;

                if (command.Contents != null)
                    command.StickyNoteModel.Contents = command.Contents;

                graphUpdater.MarkChanged(command.StickyNoteModel);
            }
        }
    }

    public class UpdateStickyNoteThemeCommand : ModelCommand<IStickyNoteModel, string>
    {
        const string k_UndoStringSingular = "Change Sticky Note Theme";
        const string k_UndoStringPlural = "Change Sticky Notes Theme";

        public UpdateStickyNoteThemeCommand()
            : base(k_UndoStringSingular) { }

        public UpdateStickyNoteThemeCommand(IStickyNoteModel[] stickyNoteModels, string theme)
            : base(k_UndoStringSingular, k_UndoStringPlural, stickyNoteModels, theme) { }

        public static void DefaultCommandHandler(GraphToolState graphToolState, UpdateStickyNoteThemeCommand command)
        {
            if (!command.Models.Any())
                return;

            graphToolState.PushUndo(command);

            using (var graphUpdater = graphToolState.GraphViewState.UpdateScope)
            {
                foreach (var noteModel in command.Models)
                {
                    noteModel.Theme = command.Value;
                }

                graphUpdater.MarkChanged(command.Models);
            }
        }
    }

    public class UpdateStickyNoteTextSizeCommand : ModelCommand<IStickyNoteModel, string>
    {
        const string k_UndoStringSingular = "Change Sticky Note Font Size";
        const string k_UndoStringPlural = "Change Sticky Notes Font Size";

        public UpdateStickyNoteTextSizeCommand()
            : base(k_UndoStringSingular) { }

        public UpdateStickyNoteTextSizeCommand(IStickyNoteModel[] stickyNoteModels, string textSize)
            : base(k_UndoStringSingular, k_UndoStringPlural, stickyNoteModels, textSize) { }

        public static void DefaultCommandHandler(GraphToolState graphToolState, UpdateStickyNoteTextSizeCommand command)
        {
            if (!command.Models.Any())
                return;

            graphToolState.PushUndo(command);

            using (var graphUpdater = graphToolState.GraphViewState.UpdateScope)
            {
                foreach (var noteModel in command.Models)
                {
                    noteModel.TextSize = command.Value;
                }

                graphUpdater.MarkChanged(command.Models);
            }
        }
    }
}
