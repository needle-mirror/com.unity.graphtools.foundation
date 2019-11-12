using System;
#if !UNITY_2019_3_OR_NEWER
using UnityEngine.UIElements;
#endif

namespace UnityEditor.VisualScripting.Editor
{
    static class UIElementExtensions
    {
#if !UNITY_2019_3_OR_NEWER
        internal static BoundLabel ReplaceWithBoundLabel(this Label pillTitleLabel)
        {
            VisualElement pillParent = pillTitleLabel.parent;
            int pillTitleIndex = pillParent.IndexOf(pillTitleLabel);
            BoundLabel boundLabel = new BoundLabel();
            boundLabel.name = pillTitleLabel.name;
            pillParent.RemoveAt(pillTitleIndex);
            pillParent.Insert(pillTitleIndex, boundLabel);
            return boundLabel;
        }

#endif
    }
}
