using System;
using JetBrains.Annotations;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.SmartSearch;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    [PublicAPI]
    public class ClassSearcherFilterProvider : ISearcherFilterProvider
    {
        readonly Stencil m_Stencil;

        public ClassSearcherFilterProvider(Stencil stencil)
        {
            m_Stencil = stencil;
        }

        public virtual SearcherFilter GetGraphSearcherFilter()
        {
            return new SearcherFilter(SearcherContext.Graph)
                .WithVisualScriptingNodes()
                .WithStickyNote();
        }

        public virtual SearcherFilter GetOutputToGraphSearcherFilter(IGTFPortModel portModel)
        {
            return new SearcherFilter(SearcherContext.Graph)
                .WithVisualScriptingNodes();
        }

        public virtual SearcherFilter GetInputToGraphSearcherFilter(IGTFPortModel portModel)
        {
            return new SearcherFilter(SearcherContext.Graph)
                .WithVisualScriptingNodes();
        }

        public virtual SearcherFilter GetEdgeSearcherFilter(IGTFEdgeModel edgeModel)
        {
            return new SearcherFilter(SearcherContext.Graph)
                .WithVisualScriptingNodesExcept(new[] { typeof(ThisNodeModel) }); // TODO : We should be able to determine if a VSNode type has input port instead of doing this
        }

        public virtual SearcherFilter GetTypeSearcherFilter()
        {
            return SearcherFilter.Empty;
        }
    }
}
