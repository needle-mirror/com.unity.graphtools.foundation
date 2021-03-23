using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.GraphViewModel;

namespace UnityEditor.VisualScripting.Model.Stencils
{
    public class MacroSearcherFilterProvider : ISearcherFilterProvider
    {
        readonly Stencil m_Stencil;

        public MacroSearcherFilterProvider(Stencil stencil)
        {
            m_Stencil = stencil;
        }

        public SearcherFilter GetGraphSearcherFilter()
        {
            return new SearcherFilter(SearcherContext.Graph)
                .WithVisualScriptingNodesExcept(new[] { typeof(IEventFunctionModel), typeof(FunctionModel) })
                .WithInlineExpression()
                .WithBinaryOperators()
                .WithUnaryOperators()
                .WithConstants()
                .WithConstructors()
                .WithMethods()
                .WithProperties()
                .WithFunctionReferences()
                .WithMacros()
                .WithStickyNote();
        }

        public SearcherFilter GetStackSearcherFilter(IStackModel stackModel)
        {
            throw new NotImplementedException("Macro does not support stacks");
        }

        public SearcherFilter GetOutputToGraphSearcherFilter(IPortModel portModel)
        {
            // TODO : Need to be handled by TypeHandle.Resolve
            TypeHandle typeHandle = portModel.DataType == TypeHandle.ThisType ? m_Stencil.GetThisType() : portModel.DataType;
            Type type = typeHandle.Resolve(m_Stencil);
            VSGraphAssetModel assetModel = portModel.AssetModel as VSGraphAssetModel;

            return new SearcherFilter(SearcherContext.Graph)
                .WithVisualScriptingNodes()
                .WithFields(type)
                .WithUnaryOperators(type, portModel.NodeModel is IConstantNodeModel)
                .WithBinaryOperators(type)
                .WithMethods(type)
                .WithProperties(type)
                .WithGraphAsset(assetModel);
        }

        public SearcherFilter GetOutputToGraphSearcherFilter(IEnumerable<IPortModel> portModel)
        {
            // TODO : Need to be handled by TypeHandle.Resolve
            TypeHandle typeHandle = portModel.First().DataType == TypeHandle.ThisType ? m_Stencil.GetThisType() : portModel.First().DataType;
            Type type = typeHandle.Resolve(m_Stencil);
            VSGraphAssetModel assetModel = portModel.First().AssetModel as VSGraphAssetModel;

            return new SearcherFilter(SearcherContext.Graph)
                .WithVisualScriptingNodes()
                .WithFields(type)
                .WithUnaryOperators(type, portModel.Any(t => t is IConstantNodeModel))
                .WithBinaryOperators(type)
                .WithMethods(type)
                .WithProperties(type)
                .WithGraphAsset(assetModel);
        }

        public SearcherFilter GetOutputToStackSearcherFilter(IPortModel portModel, IStackModel stackModel)
        {
            throw new NotImplementedException("Macro does not support stacks");
        }

        public SearcherFilter GetInputToGraphSearcherFilter(IPortModel portModel)
        {
            return new SearcherFilter(SearcherContext.Graph)
                .WithVisualScriptingNodes()
                .WithVariables(m_Stencil, portModel)
                .WithConstants(m_Stencil, portModel)
                .WithProperties(m_Stencil, portModel);
        }

        public SearcherFilter GetTypeSearcherFilter()
        {
            return SearcherFilter.Empty;
        }

        public SearcherFilter GetEdgeSearcherFilter(IEdgeModel edgeModel)
        {
            Type it = edgeModel.InputPortModel.DataType.Resolve(m_Stencil);
            IPortModel opm = edgeModel.OutputPortModel;
            TypeHandle oth = opm.DataType == TypeHandle.ThisType ? m_Stencil.GetThisType() : opm.DataType;
            Type ot = oth.Resolve(m_Stencil);

            return new SearcherFilter(SearcherContext.Graph)
                .WithVisualScriptingNodesExcept(new[] { typeof(ThisNodeModel) }) // TODO : We should be able to determine if a VSNode type has input port instead of doing this
                .WithFields(ot, it)
                .WithUnaryOperators(ot, opm.NodeModel is IConstantNodeModel)
                .WithBinaryOperators(ot)
                .WithMethods(ot, it)
                .WithProperties(ot, it, false);
        }
    }
}
