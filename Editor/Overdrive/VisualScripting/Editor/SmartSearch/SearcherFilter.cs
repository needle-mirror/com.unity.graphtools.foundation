using System.Collections.Generic;
using UnityEditor.Searcher;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.SmartSearch
{
    public class SearcherFilter
    {
        public static SearcherFilter Empty => new SearcherFilter();

        public readonly List<LuceneSearcherDatabase.Filter> LuceneFilters;

        public SearcherFilter()
        {
            LuceneFilters = new List<LuceneSearcherDatabase.Filter>();
        }

        public SearcherFilter WithFieldQuery(string field, object value, LuceneSearcherDatabase.FilterType type = LuceneSearcherDatabase.FilterType.Must)
        {
            LuceneFilters.Add(new LuceneSearcherDatabase.Filter { Field = field, Value = value, Type = type});
            return this;
        }
    }
}
