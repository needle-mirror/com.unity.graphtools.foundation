using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface IPlacematModel : IGraphElementModel, IHasTitle, IMovable, ICollapsible, IResizable, IRenamable, IDestroyable
    {
        Color Color { get; set; }
        int ZOrder { get; set; }
        IEnumerable<IGraphElementModel> HiddenElements { get; set; }
        void ResetColor();
    }
}
