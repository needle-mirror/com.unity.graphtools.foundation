using System;
using System.Collections.Generic;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    class UndoRedoTraversal : GraphTraversal
    {
        protected override void VisitEdge(IEdgeModel edgeModel)
        {
            base.VisitEdge(edgeModel);
            edgeModel.ResetPorts();
        }

        protected override void VisitNode(INodeModel nodeModel, HashSet<INodeModel> visitedNodes)
        {
            nodeModel.DefineNode();
            base.VisitNode(nodeModel, visitedNodes);
        }
    }
}
