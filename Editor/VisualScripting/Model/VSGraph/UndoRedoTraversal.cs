using System;
using System.Collections.Generic;
using UnityEditor.VisualScripting.GraphViewModel;

namespace UnityEditor.VisualScripting.Model
{
    class UndoRedoTraversal : GraphTraversal
    {
        protected override void VisitEdge(IEdgeModel edgeModel)
        {
            base.VisitEdge(edgeModel);
            ((EdgeModel)edgeModel).UndoRedoPerformed();
        }

        protected override void VisitStack(IStackModel stack, HashSet<IStackModel> visitedStacks, HashSet<INodeModel> visitedNodes)
        {
            Visit(stack);
            base.VisitStack(stack, visitedStacks, visitedNodes);
        }

        static void Visit(IGraphElementModel model)
        {
            if (model is IUndoRedoAware u)
                u.UndoRedoPerformed();
        }

        protected override void VisitNode(INodeModel nodeModel, HashSet<INodeModel> visitedNodes)
        {
            Visit(nodeModel);
            base.VisitNode(nodeModel, visitedNodes);
        }

        protected override void VisitVariableDeclaration(IVariableDeclarationModel variableDeclarationModel)
        {
            Visit(variableDeclarationModel);
            base.VisitVariableDeclaration(variableDeclarationModel);
        }
    }
}
