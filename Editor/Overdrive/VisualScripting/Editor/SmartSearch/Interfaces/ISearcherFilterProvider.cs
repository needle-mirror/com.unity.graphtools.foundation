using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.SmartSearch
{
    public interface ISearcherFilterProvider
    {
        SearcherFilter GetGraphSearcherFilter();
        SearcherFilter GetOutputToGraphSearcherFilter(IGTFPortModel portModel);
        SearcherFilter GetInputToGraphSearcherFilter(IGTFPortModel portModel);
        SearcherFilter GetTypeSearcherFilter();
        SearcherFilter GetEdgeSearcherFilter(IGTFEdgeModel edgeModel);
    }
}
