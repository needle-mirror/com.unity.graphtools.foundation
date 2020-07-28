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

        public virtual SearcherFilter GetGraphSearcherFilter() => new SearcherFilter();

        public virtual SearcherFilter GetOutputToGraphSearcherFilter(IGTFPortModel portModel) => new SearcherFilter();

        public virtual SearcherFilter GetInputToGraphSearcherFilter(IGTFPortModel portModel) => new SearcherFilter();

        public virtual SearcherFilter GetEdgeSearcherFilter(IGTFEdgeModel edgeModel) => new SearcherFilter();
    }
}
