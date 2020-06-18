using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    // PF: Fixme: Could we do without flags by initializing newRect to current rect?
    [Flags]
    public enum ResizeFlags
    {
        None = 0,
        Left = 1,
        Top = 2,
        Width = 4,
        Height = 8,
        All = Left | Top | Width | Height,
    };

    public interface IResizableGraphElement
    {
        void OnResized(Rect newRect, ResizeFlags resizeWhat);
    }
}
