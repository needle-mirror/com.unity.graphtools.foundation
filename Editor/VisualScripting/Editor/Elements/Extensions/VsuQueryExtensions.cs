using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VisualScripting.Editor
{
    public static class VsuQueryExtensions
    {
        public static T MandatoryQ<T>(this VisualElement e, string name, string className = null) where T : VisualElement
        {
            var element = e.Query<T>(name, className).Build().First();
            if (element == null)
                throw new MissingUIElementException("Cannot find mandatory UI element: " + name);
            return element;
        }

        public static VisualElement MandatoryQ(this VisualElement e, string name, string className = null)
        {
            var element = e.Query<VisualElement>(name, className).Build().First();
            if (element == null)
                throw new MissingUIElementException("Cannot find mandatory UI element: " + name);
            return element;
        }
    }
}
