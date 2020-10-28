using System;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface IOnboardingProvider
    {
        VisualElement CreateOnboardingElement(Store store);
        bool GetGraphAndObjectFromSelection(GtfoWindow window, Object selectedObject, out string assetPath,
            out GameObject boundObject);
    }
}
