using System.Collections.Generic;
using UnityEditor.Searcher;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.SmartSearch
{
    public sealed class TypeSearcherItem : SearcherItem, ISearcherItemDataProvider
    {
        public TypeHandle Type => ((TypeSearcherItemData)Data).Type;
        public ISearcherItemData Data { get; }

        public TypeSearcherItem(TypeHandle type, string name, List<SearcherItem> children = null)
            : base(name, string.Empty, children)
        {
            Data = new TypeSearcherItemData(type);
        }
    }
}
