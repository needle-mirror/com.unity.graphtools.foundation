using System;
using UnityEditor.VisualScripting.GraphViewModel;

namespace UnityEditor.VisualScripting.Editor.SmartSearch
{
    public interface ISearcherFilterProvider
    {
        SearcherFilter GetGraphSearcherFilter();
        SearcherFilter GetStackSearcherFilter(IStackModel stackModel);
        SearcherFilter GetOutputToGraphSearcherFilter(IPortModel portModel);
        SearcherFilter GetOutputToStackSearcherFilter(IPortModel portModel, IStackModel stackModel);
        SearcherFilter GetInputToGraphSearcherFilter(IPortModel portModel);
        SearcherFilter GetTypeSearcherFilter();
        SearcherFilter GetEdgeSearcherFilter(IEdgeModel edgeModel);
    }
}
