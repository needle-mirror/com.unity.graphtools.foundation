using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class CreateStickyNoteCommand : Command
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
            var stickyNote = graphToolState.GraphModel.CreateStickyNote(command.Position);
            graphToolState.MarkNew(stickyNote);
        }
    }

    public class ChangeStickyNoteLayoutCommand : ModelCommand<IStickyNoteModel, Rect>
    {
        const string k_UndoStringSingular = "Resize Sticky Note";
        const string k_UndoStringPlural = "Resize Sticky Notes";

        public ResizeFlags ResizeWhat;

        public ChangeStickyNoteLayoutCommand()
            : base(k_UndoStringSingular) {}

        public ChangeStickyNoteLayoutCommand(IStickyNoteModel stickyNoteModel, Rect position, ResizeFlags resizeWhat)
            : base(k_UndoStringSingular, k_UndoStringPlural, new[] { stickyNoteModel }, position)
        {
            ResizeWhat = resizeWhat;
        }

        public static void DefaultCommandHandler(GraphToolState graphToolState, ChangeStickyNoteLayoutCommand command)
        {
            if (!command.Models.Any())
                return;

            if (command.ResizeWhat == ResizeFlags.None)
                return;

            graphToolState.PushUndo(command);

            foreach (var noteModel in command.Models)
            {
                var newRect = noteModel.PositionAndSize;
                if ((command.ResizeWhat & ResizeFlags.Left) == ResizeFlags.Left)
                {
                    newRect.x = command.Value.x;
                }
                if ((command.ResizeWhat & ResizeFlags.Top) == ResizeFlags.Top)
                {
                    newRect.y = command.Value.y;
                }
                if ((command.ResizeWhat & ResizeFlags.Width) == ResizeFlags.Width)
                {
                    newRect.width = command.Value.width;
                }
                if ((command.ResizeWhat & ResizeFlags.Height) == ResizeFlags.Height)
                {
                    newRect.height = command.Value.height;
                }

                noteModel.PositionAndSize = newRect;
            }
            graphToolState.MarkChanged(command.Models);
        }
    }

    public class UpdateStickyNoteCommand : Command
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

            if (command.Title != null)
                command.StickyNoteModel.Title = command.Title;

            if (command.Contents != null)
                command.StickyNoteModel.Contents = command.Contents;

            graphToolState.MarkChanged(command.StickyNoteModel);
        }
    }

    public class UpdateStickyNoteThemeCommand : ModelCommand<IStickyNoteModel, string>
    {
        const string k_UndoStringSingular = "Change Sticky Note Theme";
        const string k_UndoStringPlural = "Change Sticky Notes Theme";

        public UpdateStickyNoteThemeCommand()
            : base(k_UndoStringSingular) {}

        public UpdateStickyNoteThemeCommand(IStickyNoteModel[] stickyNoteModels, string theme)
            : base(k_UndoStringSingular, k_UndoStringPlural, stickyNoteModels, theme) {}

        public static void DefaultCommandHandler(GraphToolState graphToolState, UpdateStickyNoteThemeCommand command)
        {
            if (!command.Models.Any())
                return;

            graphToolState.PushUndo(command);

            foreach (var noteModel in command.Models)
            {
                noteModel.Theme = command.Value;
            }
            graphToolState.MarkChanged(command.Models);
        }
    }

    public class UpdateStickyNoteTextSizeCommand : ModelCommand<IStickyNoteModel, string>
    {
        const string k_UndoStringSingular = "Change Sticky Note Font Size";
        const string k_UndoStringPlural = "Change Sticky Notes Font Size";

        public UpdateStickyNoteTextSizeCommand()
            : base(k_UndoStringSingular) {}

        public UpdateStickyNoteTextSizeCommand(IStickyNoteModel[] stickyNoteModels, string textSize)
            : base(k_UndoStringSingular, k_UndoStringPlural, stickyNoteModels, textSize) {}

        public static void DefaultCommandHandler(GraphToolState graphToolState, UpdateStickyNoteTextSizeCommand command)
        {
            if (!command.Models.Any())
                return;

            graphToolState.PushUndo(command);

            foreach (var noteModel in command.Models)
            {
                noteModel.TextSize = command.Value;
            }
            graphToolState.MarkChanged(command.Models);
        }
    }
}
