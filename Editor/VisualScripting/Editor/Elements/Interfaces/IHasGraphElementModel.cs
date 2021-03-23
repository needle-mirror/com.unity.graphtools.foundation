using System;
using UnityEditor.VisualScripting.GraphViewModel;

namespace UnityEditor.VisualScripting.Editor
{
    public interface IHasGraphElementModel
    {
        IGraphElementModel GraphElementModel { get; }
    }
}
