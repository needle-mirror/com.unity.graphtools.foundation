using System;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public interface IHighlightable
    {
        bool Highlighted { get; set; }
        bool ShouldHighlightItemUsage(IGraphElementModel graphElementModel);
    }
}
