using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public static class VisualElementExtensions
    {
        public static void PrefixRemoveFromClassList(this VisualElement ve, string classNamePrefix)
        {
            var toRemove = ve.GetClasses().Where(c => c.StartsWith(classNamePrefix)).ToList();
            foreach (var c in toRemove)
            {
                ve.RemoveFromClassList(c);
            }
        }

        public static void ReplaceManipulator<T>(this VisualElement ve, ref T manipulator, T newManipulator) where T : Manipulator
        {
            ve.RemoveManipulator(manipulator);
            manipulator = newManipulator;
            ve.AddManipulator(newManipulator);
        }

        public static Rect GetRect(this VisualElement ve)
        {
            return new Rect(0.0f, 0.0f, ve.layout.width, ve.layout.height);
        }
    }
}
