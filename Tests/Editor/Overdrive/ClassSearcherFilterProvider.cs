using System;
using System.Collections.Generic;
using JetBrains.Annotations;

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

        public virtual SearcherFilter GetGraphSearcherFilter() => new SearcherFilter();

        public virtual SearcherFilter GetOutputToGraphSearcherFilter(IPortModel portModel) => new SearcherFilter();
        public virtual SearcherFilter GetOutputToGraphSearcherFilter(IEnumerable<IPortModel> portModel) => new SearcherFilter();

        public virtual SearcherFilter GetInputToGraphSearcherFilter(IPortModel portModel) => new SearcherFilter();
        public virtual SearcherFilter GetInputToGraphSearcherFilter(IEnumerable<IPortModel> portModels) => new SearcherFilter();

        public virtual SearcherFilter GetEdgeSearcherFilter(IEdgeModel edgeModel) => new SearcherFilter();
    }
}
