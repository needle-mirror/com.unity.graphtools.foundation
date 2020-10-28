using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class CreateStickyNoteAction : BaseAction
    {
        public readonly Rect Position;

        public CreateStickyNoteAction()
        {
            UndoString = "Create Sticky Note";
        }

        public CreateStickyNoteAction(string name, Rect position) : this()
        {
            Position = position;
        }

        public static void DefaultReducer(State previousState, CreateStickyNoteAction action)
        {
            previousState.PushUndo(action);
            previousState.CurrentGraphModel.CreateStickyNote(action.Position);
        }
    }

    public class ChangeStickyNoteLayoutAction : ModelAction<IStickyNoteModel, Rect>
    {
        const string k_UndoStringSingular = "Resize Sticky Note";
        const string k_UndoStringPlural = "Resize Sticky Notes";

        public ResizeFlags ResizeWhat;

        public ChangeStickyNoteLayoutAction()
            : base(k_UndoStringSingular) {}

        public ChangeStickyNoteLayoutAction(IStickyNoteModel stickyNoteModel, Rect position, ResizeFlags resizeWhat)
            : base(k_UndoStringSingular, k_UndoStringPlural, new[] { stickyNoteModel }, position)
        {
            ResizeWhat = resizeWhat;
        }

        public static void DefaultReducer(State previousState, ChangeStickyNoteLayoutAction action)
        {
            if (!action.Models.Any())
                return;

            if (action.ResizeWhat == ResizeFlags.None)
                return;

            previousState.PushUndo(action);

            foreach (var noteModel in action.Models)
            {
                var newRect = noteModel.PositionAndSize;
                if ((action.ResizeWhat & ResizeFlags.Left) == ResizeFlags.Left)
                {
                    newRect.x = action.Value.x;
                }
                if ((action.ResizeWhat & ResizeFlags.Top) == ResizeFlags.Top)
                {
                    newRect.y = action.Value.y;
                }
                if ((action.ResizeWhat & ResizeFlags.Width) == ResizeFlags.Width)
                {
                    newRect.width = action.Value.width;
                }
                if ((action.ResizeWhat & ResizeFlags.Height) == ResizeFlags.Height)
                {
                    newRect.height = action.Value.height;
                }

                noteModel.PositionAndSize = newRect;
                previousState.MarkForUpdate(UpdateFlags.UpdateView, noteModel);
            }
        }
    }

    public class UpdateStickyNoteAction : BaseAction
    {
        public readonly string Title;
        public readonly string Contents;
        public readonly IStickyNoteModel StickyNoteModel;

        public UpdateStickyNoteAction()
        {
            UndoString = "Update Sticky Note Content";
        }

        public UpdateStickyNoteAction(IStickyNoteModel stickyNoteModel, string title, string contents) : this()
        {
            StickyNoteModel = stickyNoteModel;
            Title = title;
            Contents = contents;
        }

        public static void DefaultReducer(State previousState, UpdateStickyNoteAction action)
        {
            if (action.Title == null && action.Contents == null)
                return;

            previousState.PushUndo(action);

            if (action.Title != null)
                action.StickyNoteModel.Title = action.Title;

            if (action.Contents != null)
                action.StickyNoteModel.Contents = action.Contents;

            previousState.MarkForUpdate(UpdateFlags.UpdateView, action.StickyNoteModel);
        }
    }

    public class UpdateStickyNoteThemeAction : ModelAction<IStickyNoteModel, string>
    {
        const string k_UndoStringSingular = "Change Sticky Note Theme";
        const string k_UndoStringPlural = "Change Sticky Notes Theme";

        public UpdateStickyNoteThemeAction()
            : base(k_UndoStringSingular) {}

        public UpdateStickyNoteThemeAction(IStickyNoteModel[] stickyNoteModels, string theme)
            : base(k_UndoStringSingular, k_UndoStringPlural, stickyNoteModels, theme) {}

        public static void DefaultReducer(State previousState, UpdateStickyNoteThemeAction action)
        {
            if (!action.Models.Any())
                return;

            previousState.PushUndo(action);

            foreach (var noteModel in action.Models)
            {
                noteModel.Theme = action.Value;
                previousState.MarkForUpdate(UpdateFlags.UpdateView, noteModel);
            }
        }
    }

    public class UpdateStickyNoteTextSizeAction : ModelAction<IStickyNoteModel, string>
    {
        const string k_UndoStringSingular = "Change Sticky Note Font Size";
        const string k_UndoStringPlural = "Change Sticky Notes Font Size";

        public UpdateStickyNoteTextSizeAction()
            : base(k_UndoStringSingular) {}

        public UpdateStickyNoteTextSizeAction(IStickyNoteModel[] stickyNoteModels, string textSize)
            : base(k_UndoStringSingular, k_UndoStringPlural, stickyNoteModels, textSize) {}

        public static void DefaultReducer(State previousState, UpdateStickyNoteTextSizeAction action)
        {
            if (!action.Models.Any())
                return;

            previousState.PushUndo(action);

            foreach (var noteModel in action.Models)
            {
                noteModel.TextSize = action.Value;
                previousState.MarkForUpdate(UpdateFlags.UpdateView, noteModel);
            }
        }
    }
}
