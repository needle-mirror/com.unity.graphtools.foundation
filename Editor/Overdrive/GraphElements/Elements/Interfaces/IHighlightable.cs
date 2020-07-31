using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface IHighlightable
    {
        bool Highlighted { get; set; }
        bool ShouldHighlightItemUsage(IGTFGraphElementModel graphElementModel);
    }
}
