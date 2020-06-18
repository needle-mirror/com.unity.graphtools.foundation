using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    class UndoRedoTraversal : GraphTraversal
    {
        protected override void VisitEdge(IEdgeModel edgeModel)
        {
            base.VisitEdge(edgeModel);
            ((EdgeModel)edgeModel).UndoRedoPerformed();
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
