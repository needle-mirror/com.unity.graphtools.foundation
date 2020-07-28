using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Model
{
    public interface IGTFPlacematModel : IGTFGraphElementModel, IHasTitle, ISelectable, IPositioned, IDeletable, ICopiable, ICollapsible, IResizable, IRenamable, IDestroyable
    {
        Color Color { get; set; }
        int ZOrder { get; set; }
        IEnumerable<IGTFGraphElementModel> HiddenElements { get; set; }
    }
}
