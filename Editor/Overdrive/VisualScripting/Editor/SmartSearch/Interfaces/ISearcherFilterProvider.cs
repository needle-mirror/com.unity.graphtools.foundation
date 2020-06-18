using System;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.SmartSearch
{
    public interface ISearcherFilterProvider
    {
        SearcherFilter GetGraphSearcherFilter();
        SearcherFilter GetOutputToGraphSearcherFilter(IPortModel portModel);
        SearcherFilter GetInputToGraphSearcherFilter(IPortModel portModel);
        SearcherFilter GetTypeSearcherFilter();
        SearcherFilter GetEdgeSearcherFilter(IEdgeModel edgeModel);
    }
}
