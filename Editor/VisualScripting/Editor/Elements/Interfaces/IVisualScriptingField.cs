using System;
using UnityEditor.VisualScripting.GraphViewModel;

namespace UnityEditor.VisualScripting.Editor
{
    public interface IVisualScriptingField
    {
        IGraphElementModel GraphElementModel { get; }
        IGraphElementModel ExpandableGraphElementModel { get; }
        void Expand();
        bool CanInstantiateInGraph();
    }
}
