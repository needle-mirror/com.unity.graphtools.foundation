using System;
using JetBrains.Annotations;
using UnityEditor.Searcher;

namespace UnityEditor.VisualScripting.Editor.SmartSearch
{
    public static class SearcherItemExtensions
    {
        [CanBeNull]
        public static SearcherItem Find(this SearcherItem item, string name)
        {
            if (item.Name == name)
                return item;

            foreach (var child in item.Children)
            {
                var found = child.Find(name);
                if (found != null)
                    return found;
            }

            return null;
        }
    }
}
