#if UNITY_2020_1_OR_NEWER
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VisualScripting.GraphViewModel
{
    public interface IPlacematModel : IGraphElementModel, IUndoRedoAware
    {
        string Title { get; }
        Rect Position { get; }
        Color Color { get; }
        bool Collapsed { get; }
        int ZOrder { get; }
        List<string> HiddenElementsGuid { get; }
        bool Destroyed { get; }
    }
}
#endif
