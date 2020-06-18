using System;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public interface IHasGraphElementModel
    {
        IGraphElementModel GraphElementModel { get; }
    }
}
