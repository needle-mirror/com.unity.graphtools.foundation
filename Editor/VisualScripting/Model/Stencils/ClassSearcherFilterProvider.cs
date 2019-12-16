using System;
using JetBrains.Annotations;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.GraphViewModel;

namespace UnityEditor.VisualScripting.Model.Stencils
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
                .WithEmptyFunction()
                .WithStack()
                .WithInlineExpression()
                .WithBinaryOperators()
                .WithUnaryOperators()
                .WithControlFlows()
                .WithConstants()
                .WithConstructors()
                .WithMethods()
                .WithProperties()
                .WithFunctionReferences()
                .WithMacros()
                .WithStickyNote()
                .WithConstantFields();
        }

        public virtual SearcherFilter GetStackSearcherFilter(IStackModel stackModel)
        {
            return new SearcherFilter(SearcherContext.Stack)
                .WithVisualScriptingNodes(stackModel)
                .WithUnaryOperators()
                .WithControlFlows(stackModel)
                .WithProperties()
                .WithMethods()
                .WithFunctionReferences()
                .WithMacros();
        }

        public virtual SearcherFilter GetOutputToGraphSearcherFilter(IPortModel portModel)
        {
            // TODO : Need to be handled by TypeHandle.Resolve
            TypeHandle typeHandle = portModel.DataType == TypeHandle.ThisType ? m_Stencil.GetThisType() : portModel.DataType;
            Type type = typeHandle.Resolve(m_Stencil);
            GraphAssetModel assetModel = portModel.AssetModel as GraphAssetModel;

            return new SearcherFilter(SearcherContext.Graph)
                .WithVisualScriptingNodes()
                .WithFields(type)
                .WithUnaryOperators(type, portModel.NodeModel is IConstantNodeModel)
                .WithBinaryOperators(type)
                .WithMethods(type)
                .WithProperties(type)
                .WithGraphAsset(assetModel);
        }

        public virtual SearcherFilter GetOutputToStackSearcherFilter(IPortModel portModel, IStackModel stackModel)
        {
            // TODO : Need to be handled by TypeHandle.Resolve
            TypeHandle typeHandle = portModel.DataType == TypeHandle.ThisType ? m_Stencil.GetThisType() : portModel.DataType;
            Type type = typeHandle.Resolve(m_Stencil);
            GraphAssetModel assetModel = portModel.AssetModel as GraphAssetModel;

            return new SearcherFilter(SearcherContext.Stack)
                .WithVisualScriptingNodes()
                .WithFields(type)
                .WithUnaryOperators(type)
                .WithIfConditions(typeHandle, stackModel)
                .WithMethods(type)
                .WithProperties(type)
                .WithGraphAsset(assetModel);
        }

        public virtual SearcherFilter GetInputToGraphSearcherFilter(IPortModel portModel)
        {
            var dataType = portModel.DataType.Resolve(m_Stencil);
            return new SearcherFilter(SearcherContext.Graph)
                .WithVisualScriptingNodesExcept(new[] { typeof(GetPropertyGroupNodeModel) })
                .WithVariables(m_Stencil, portModel)
                .WithConstants(m_Stencil, portModel)
                .WithProperties(m_Stencil, portModel)
                .WithUnaryOperators(dataType)
                .WithBinaryOperators(dataType)
                .WithConstantFields(dataType);
        }

        public virtual SearcherFilter GetEdgeSearcherFilter(IEdgeModel edgeModel)
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

        public virtual SearcherFilter GetTypeSearcherFilter()
        {
            return SearcherFilter.Empty;
        }
    }
}
