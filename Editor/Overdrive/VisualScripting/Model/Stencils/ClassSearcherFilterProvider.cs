using System;
using JetBrains.Annotations;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.SmartSearch;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
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
                .WithConstants()
                .WithMacros()
                .WithStickyNote();
        }

        public virtual SearcherFilter GetOutputToGraphSearcherFilter(IPortModel portModel)
        {
            GraphAssetModel assetModel = portModel.AssetModel as GraphAssetModel;
            return new SearcherFilter(SearcherContext.Graph)
                .WithVisualScriptingNodes()
                .WithGraphAsset(assetModel);
        }

        public virtual SearcherFilter GetInputToGraphSearcherFilter(IPortModel portModel)
        {
            return new SearcherFilter(SearcherContext.Graph)
                .WithVisualScriptingNodes()
                .WithVariables(m_Stencil, portModel)
                .WithConstants(m_Stencil, portModel);
        }

        public virtual SearcherFilter GetEdgeSearcherFilter(IEdgeModel edgeModel)
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
