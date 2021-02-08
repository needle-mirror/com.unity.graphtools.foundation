using System;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface IModelUIPart
    {
        string PartName { get; }
        VisualElement Root { get; }

        void BuildUI(VisualElement parent);
        void PostBuildUI();
        void UpdateFromModel();
        void OwnerAddedToView();
        void OwnerRemovedFromView();
    }
}
