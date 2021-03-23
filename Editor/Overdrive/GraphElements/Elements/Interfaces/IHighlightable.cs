using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface IHighlightable : IGraphElement
    {
        bool Highlighted { get; set; }
        bool ShouldHighlightItemUsage(IGraphElementModel graphElementModel);
    }
}
