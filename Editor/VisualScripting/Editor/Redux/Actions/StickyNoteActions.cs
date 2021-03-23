using System;
using System.Collections.Generic;
using UnityEditor.EditorCommon.Redux;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEngine;

namespace UnityEditor.VisualScripting.Editor
{
    public class CreateStickyNoteAction : IAction
    {
        public readonly Rect Position;

        public CreateStickyNoteAction(string name, Rect position)
        {
            Position = position;
        }
    }

    public class ResizeStickyNoteAction : IAction
    {
        public readonly Rect Position;
        public readonly IStickyNoteModel StickyNoteModel;

        public ResizeStickyNoteAction(IStickyNoteModel stickyNoteModel, Rect position)
        {
            StickyNoteModel = stickyNoteModel;
            Position = position;
        }
    }

    public class UpdateStickyNoteAction : IAction
    {
        public readonly string Title;
        public readonly string Contents;
        public readonly IStickyNoteModel StickyNoteModel;

        public UpdateStickyNoteAction(IStickyNoteModel stickyNoteModel, string title, string contents)
        {
            StickyNoteModel = stickyNoteModel;
            Title = title;
            Contents = contents;
        }
    }

    public class UpdateStickyNoteThemeAction : IAction
    {
        public readonly StickyNoteColorTheme Theme;
        public readonly List<IStickyNoteModel> StickyNoteModels;

        public UpdateStickyNoteThemeAction(List<IStickyNoteModel> stickyNoteModels, StickyNoteColorTheme theme)
        {
            StickyNoteModels = stickyNoteModels;
            Theme = theme;
        }
    }

    public class UpdateStickyNoteTextSizeAction : IAction
    {
        public readonly StickyNoteTextSize TextSize;
        public readonly List<IStickyNoteModel> StickyNoteModels;

        public UpdateStickyNoteTextSizeAction(List<IStickyNoteModel> stickyNoteModels, StickyNoteTextSize textSize)
        {
            StickyNoteModels = stickyNoteModels;
            TextSize = textSize;
        }
    }
}
