using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    public abstract class GenericStickyNoteAction<DataType> : GenericModelAction<IGTFStickyNoteModel, DataType>
    {
        public GenericStickyNoteAction(IGTFStickyNoteModel[] models, DataType value)
            : base(models, value) {}
    }

    public class CreateStickyNoteAction : IAction
    {
        public readonly Rect Position;

        public CreateStickyNoteAction(string name, Rect position)
        {
            Position = position;
        }

        public static TState DefaultReducer<TState>(TState previousState, CreateStickyNoteAction action) where TState : State
        {
            return previousState;
        }
    }

    public class ResizeStickyNoteAction : GenericStickyNoteAction<Rect>
    {
        public ResizeFlags ResizeWhat;

        public ResizeStickyNoteAction(IGTFStickyNoteModel stickyNoteModel, Rect position, ResizeFlags resizeWhat)
            : base(new[] { stickyNoteModel }, position)
        {
            ResizeWhat = resizeWhat;
        }

        public static TState DefaultReducer<TState>(TState previousState, ResizeStickyNoteAction action) where TState : State
        {
            var newRect = action.Models[0].PositionAndSize;
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

            action.Models[0].PositionAndSize = newRect;

            return previousState;
        }
    }

    public class UpdateStickyNoteAction : IAction
    {
        public readonly string Title;
        public readonly string Contents;
        public readonly IGTFStickyNoteModel StickyNoteModel;

        public UpdateStickyNoteAction(IGTFStickyNoteModel stickyNoteModel, string title, string contents)
        {
            StickyNoteModel = stickyNoteModel;
            Title = title;
            Contents = contents;
        }

        public static TState DefaultReducer<TState>(TState previousState, UpdateStickyNoteAction action) where TState : State
        {
            if (action.Title != null)
                action.StickyNoteModel.Title = action.Title;

            if (action.Contents != null)
                action.StickyNoteModel.Contents = action.Contents;

            return previousState;
        }
    }

    public class UpdateStickyNoteThemeAction : GenericStickyNoteAction<string>
    {
        public UpdateStickyNoteThemeAction(IGTFStickyNoteModel[] stickyNoteModels, string theme)
            : base(stickyNoteModels, theme) {}

        public static TState DefaultReducer<TState>(TState previousState, UpdateStickyNoteThemeAction action) where TState : State
        {
            foreach (var model in action.Models)
                model.Theme = action.Value;

            return previousState;
        }
    }

    public class UpdateStickyNoteTextSizeAction : GenericStickyNoteAction<string>
    {
        public UpdateStickyNoteTextSizeAction(IGTFStickyNoteModel[] stickyNoteModels, string textSize)
            : base(stickyNoteModels, textSize) {}

        public static TState DefaultReducer<TState>(TState previousState, UpdateStickyNoteTextSizeAction action) where TState : State
        {
            foreach (var model in action.Models)
                model.TextSize = action.Value;

            return previousState;
        }
    }
}
