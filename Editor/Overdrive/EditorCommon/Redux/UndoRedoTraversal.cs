using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    class UndoRedoTraversal : GraphTraversal
    {
        protected override void VisitEdge(IGTFEdgeModel edgeModel)
        {
            base.VisitEdge(edgeModel);
            edgeModel.ResetPorts();
        }

        protected override void VisitNode(IGTFNodeModel nodeModel, HashSet<IGTFNodeModel> visitedNodes)
        {
            nodeModel.DefineNode();
            base.VisitNode(nodeModel, visitedNodes);
        }
    }
}
