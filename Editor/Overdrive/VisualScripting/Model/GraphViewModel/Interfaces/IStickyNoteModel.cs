using JetBrains.Annotations;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel
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

    public interface IStickyNoteModel : IGraphElementModelWithGuid
    {
        bool Destroyed { get; }
    }
}
