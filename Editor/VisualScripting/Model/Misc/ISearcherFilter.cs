using System;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.GraphViewModel;

namespace UnityEditor.VisualScripting.Model
{
    public interface ISearcherFilter
    {
        SearcherFilter GetFilter(INodeModel model);
    }
}
