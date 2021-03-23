using System;
using System.Collections.Generic;
using UnityEditor.Searcher;
using UnityEditor.VisualScripting.Model.Stencils;

namespace UnityEditor.VisualScripting.Editor.SmartSearch
{
    public sealed class TypeSearcherItem : SearcherItem, ISearcherItemDataProvider
    {
        public TypeHandle Type => ((TypeSearcherItemData)Data).Type;
        public ISearcherItemData Data { get; }

        public TypeSearcherItem(TypeHandle type, string name, List<SearcherItem> children = null)
            : base(name, string.Empty, children)
        {
            Data = new TypeSearcherItemData(type, SearcherItemTarget.Type);
        }
    }
}
