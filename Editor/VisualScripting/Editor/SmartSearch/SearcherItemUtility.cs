using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor.Searcher;
using UnityEngine.Assertions;

namespace UnityEditor.VisualScripting.Editor.SmartSearch
{
    public static class SearcherItemUtility
    {
        [NotNull]
        public static SearcherItem GetItemFromPath([NotNull] List<SearcherItem> items, string path)
        {
            Assert.IsFalse(string.IsNullOrEmpty(path));

            string[] hierarchy = path.Split('/');
            SearcherItem item = null;
            SearcherItem parent = null;

            for (var i = 0; i < hierarchy.Length; ++i)
            {
                string s = hierarchy[i];

                if (i == 0 && s == "/" || s == string.Empty)
                    continue;

                List<SearcherItem> children = parent != null ? parent.Children : items;
                item = children.Find(x => x.Name == s);

                if (item == null)
                {
                    item = new SearcherItem(s);

                    if (parent != null)
                    {
                        parent.AddChild(item);
                    }
                    else
                    {
                        children.Add(item);
                    }
                }

                parent = item;
            }

            return item ?? throw new InvalidOperationException(
                "[SearcherItemUtility.GetItemFromPath] : Returned item cannot be null"
            );
        }
    }
}
