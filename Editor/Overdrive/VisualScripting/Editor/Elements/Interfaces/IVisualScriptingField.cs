using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public interface IVisualScriptingField
    {
        IGTFGraphElementModel Model { get; }
        IGTFGraphElementModel ExpandableGraphElementModel { get; }
        void Expand();
        bool CanInstantiateInGraph();
    }
}
