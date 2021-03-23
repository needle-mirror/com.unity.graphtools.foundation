using System;
using System.Collections.Generic;
using UnityEditor.Searcher;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;

namespace UnityEditor.VisualScripting.Editor.SmartSearch
{
    public interface ISearcherDatabaseProvider
    {
        List<SearcherDatabase> GetGraphElementsSearcherDatabases();
        List<SearcherDatabase> GetReferenceItemsSearcherDatabases();
        List<SearcherDatabase> GetTypesSearcherDatabases();
        List<SearcherDatabase> GetTypeMembersSearcherDatabases(TypeHandle typeHandle);
        List<SearcherDatabase> GetGraphVariablesSearcherDatabases(IGraphModel graphModel, IFunctionModel functionModel = null);
        List<SearcherDatabase> GetDynamicSearcherDatabases(IPortModel portModel);
        void ClearGraphElementsSearcherDatabases();
        void ClearReferenceItemsSearcherDatabases();
        void ClearTypesItemsSearcherDatabases();
        void ClearTypeMembersSearcherDatabases();
        void ClearGraphVariablesSearcherDatabases();
    }
}
