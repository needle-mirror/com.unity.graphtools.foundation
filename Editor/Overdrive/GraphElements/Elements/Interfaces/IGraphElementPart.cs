using System;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    public interface IGraphElementPart
    {
        string PartName { get; }
        GraphElementPartList PartList { get; }
        VisualElement Root { get; }

        void BuildUI(VisualElement parent);
        void PostBuildUI();
        void UpdateFromModel();
    }
}
