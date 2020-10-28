using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface IHighlightable
    {
        bool Highlighted { get; set; }
        bool ShouldHighlightItemUsage(IGraphElementModel graphElementModel);
    }
}
