namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface IStickyNoteModel : IGraphElementModel, IMovable, IHasTitle, IRenamable, IResizable, IDestroyable
    {
        string Contents { get; set; }
        string Theme { get; set; }
        string TextSize { get; set; }
    }
}
