using System;
using System.Collections.Generic;
using UnityEditor.Searcher;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.SmartSearch
{
    public interface ISearcherDatabaseProvider
    {
        List<SearcherDatabase> GetGraphElementsSearcherDatabases();
        List<SearcherDatabase> GetTypesSearcherDatabases();
        List<SearcherDatabase> GetGraphVariablesSearcherDatabases(IGraphModel graphModel);
        List<SearcherDatabase> GetDynamicSearcherDatabases(IPortModel portModel);
        void ClearGraphElementsSearcherDatabases();
        void ClearTypesItemsSearcherDatabases();
        void ClearTypeMembersSearcherDatabases();
        void ClearGraphVariablesSearcherDatabases();
    }
}
