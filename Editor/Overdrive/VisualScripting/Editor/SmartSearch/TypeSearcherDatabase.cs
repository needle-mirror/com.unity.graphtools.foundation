using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor.Searcher;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.SmartSearch
{
    [PublicAPI]
    public class TypeSearcherDatabase
    {
        public static SearcherDatabase FromTypes(Stencil stencil, IEnumerable<Type> types)
        {
            List<SearcherItem> searcherItems = new List<SearcherItem>();
            foreach (var type in types)
            {
                var typeMetadata = new TypeMetadata(type.GenerateTypeHandle(), type);
                var classItem = new TypeSearcherItem(type.GenerateTypeHandle(), typeMetadata.FriendlyName);
                searcherItems.TryAddClassItem(classItem, typeMetadata);
            }
            return SearcherDatabase.Create(searcherItems , null, false);
        }

        public static SearcherDatabase FromItems(IEnumerable<SearcherItem> items)
        {
            return SearcherDatabase.Create(items.ToList(), null, false);
        }
    }
}
