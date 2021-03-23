using System;
using JetBrains.Annotations;
using UnityEngine;

namespace UnityEditor.VisualScripting.GraphViewModel
{
    [PublicAPI]
    public enum StickyNoteTextSize
    {
        Small,
        Medium,
        Large,
        Huge
    }

    [PublicAPI]
    public enum StickyNoteColorTheme
    {
        Classic,
        Dark,
        Orange,
        Green,
        Blue,
        Red,
        Purple,
        Teal
    }

    public interface IStickyNoteModel : IGraphElementModel
    {
        string Title { get; }
        string Contents { get; }
        Rect Position { get; }
        StickyNoteColorTheme Theme { get; }
        StickyNoteTextSize TextSize { get; }
        bool Destroyed { get; }
    }
}
