using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
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
    }
}
