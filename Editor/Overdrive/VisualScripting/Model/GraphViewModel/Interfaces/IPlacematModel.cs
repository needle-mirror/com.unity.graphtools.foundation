using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel
{
    public interface IPlacematModel : IGraphElementModelWithGuid, IUndoRedoAware
    {
        Color Color { get; }
        int ZOrder { get; set; }
        List<string> HiddenElementsGuid { get; }
        bool Destroyed { get; }
    }
}
