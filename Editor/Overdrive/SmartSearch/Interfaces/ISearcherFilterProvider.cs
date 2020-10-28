using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface ISearcherFilterProvider
    {
        SearcherFilter GetGraphSearcherFilter();
        SearcherFilter GetOutputToGraphSearcherFilter(IPortModel portModel);
        SearcherFilter GetInputToGraphSearcherFilter(IPortModel portModel);
        SearcherFilter GetEdgeSearcherFilter(IEdgeModel edgeModel);
    }
}
