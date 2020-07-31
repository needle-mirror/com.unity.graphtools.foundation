using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.Searcher;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.SmartSearch
{
    public interface ISearcherDatabaseProvider
    {
        List<SearcherDatabase> GetGraphElementsSearcherDatabases(IGTFGraphModel graphModel);
        List<SearcherDatabase> GetTypesSearcherDatabases();
        List<SearcherDatabase> GetGraphVariablesSearcherDatabases(IGTFGraphModel graphModel);
        List<SearcherDatabase> GetDynamicSearcherDatabases(IGTFPortModel portModel);
        void ClearGraphElementsSearcherDatabases();
        void ClearTypesItemsSearcherDatabases();
        void ClearTypeMembersSearcherDatabases();
        void ClearGraphVariablesSearcherDatabases();
    }
}
