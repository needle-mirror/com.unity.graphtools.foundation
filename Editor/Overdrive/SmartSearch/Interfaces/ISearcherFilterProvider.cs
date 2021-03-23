using System;
using System.Collections.Generic;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface ISearcherFilterProvider
    {
        SearcherFilter GetGraphSearcherFilter();
        SearcherFilter GetOutputToGraphSearcherFilter(IEnumerable<IPortModel> portModels);
        SearcherFilter GetOutputToGraphSearcherFilter(IPortModel portModel);
        SearcherFilter GetInputToGraphSearcherFilter(IEnumerable<IPortModel> portModels);
        SearcherFilter GetInputToGraphSearcherFilter(IPortModel portModel);
        SearcherFilter GetEdgeSearcherFilter(IEdgeModel edgeModel);
    }
}
