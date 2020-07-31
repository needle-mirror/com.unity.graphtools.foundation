using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    class UndoRedoTraversal : GraphTraversal
    {
        protected override void VisitEdge(IGTFEdgeModel edgeModel)
        {
            base.VisitEdge(edgeModel);
            ((EdgeModel)edgeModel).ResetPorts();
        }

        protected override void VisitNode(IGTFNodeModel nodeModel, HashSet<IGTFNodeModel> visitedNodes)
        {
            nodeModel.DefineNode();
            base.VisitNode(nodeModel, visitedNodes);
        }
    }
}
