using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public static class VsuQueryExtensions
    {
        // PF move to bridge and use Unity version.
        public static T MandatoryQ<T>(this VisualElement e, string name = null, string className = null) where T : VisualElement
        {
            var element = e.Query<T>(name, className).Build().First();
            if (element == null)
                throw new MissingUIElementException("Cannot find mandatory UI element: " + name);
            return element;
        }

        // PF move to bridge and use Unity version.
        public static VisualElement MandatoryQ(this VisualElement e, string name = null, string className = null)
        {
            var element = e.Query<VisualElement>(name, className).Build().First();
            if (element == null)
                throw new MissingUIElementException("Cannot find mandatory UI element: " + name);
            return element;
        }
    }
}
