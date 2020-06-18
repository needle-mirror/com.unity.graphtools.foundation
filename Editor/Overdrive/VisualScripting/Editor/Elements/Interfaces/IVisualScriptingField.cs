using System;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public interface IVisualScriptingField
    {
        IGraphElementModel GraphElementModel { get; }
        IGraphElementModel ExpandableGraphElementModel { get; }
        void Expand();
        bool CanInstantiateInGraph();
    }
}
