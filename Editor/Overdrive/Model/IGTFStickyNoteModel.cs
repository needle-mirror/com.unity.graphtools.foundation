using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Model
{
    public interface IGTFStickyNoteModel : IGTFGraphElementModel, ISelectable, IPositioned, IDeletable, ICopiable, IHasTitle, IRenamable, IResizable, IDestroyable
    {
        string Contents { get; set; }
        string Theme { get; set; }
        string TextSize { get; set; }
    }
}
