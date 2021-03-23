using System;
using UnityEditor.VisualScripting.GraphViewModel;

namespace UnityEditor.VisualScripting.Editor
{
    public interface IHighlightable
    {
        bool Highlighted { get; set; }
        bool ShouldHighlightItemUsage(IGraphElementModel graphElementModel);
    }
}
