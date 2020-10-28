using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public interface IVisualScriptingField
    {
        IGraphElementModel Model { get; }
        IGraphElementModel ExpandableGraphElementModel { get; }
        void Expand();
        bool CanInstantiateInGraph();
    }
}
