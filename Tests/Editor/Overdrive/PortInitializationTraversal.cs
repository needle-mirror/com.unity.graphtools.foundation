using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public class PortInitializationTraversal : GraphTraversal
    {
        public List<Action<INodeModel>> Callbacks = new List<Action<INodeModel>>();
        protected override void VisitNode(INodeModel nodeModel, HashSet<INodeModel> visitedNodes)
        {
            // recurse first
            base.VisitNode(nodeModel, visitedNodes);

            if (nodeModel == null)
                return;

            foreach (var callback in Callbacks)
                callback(nodeModel);

            // do after left recursion, so the leftmost node is processed first
            foreach (var inputPortModel in nodeModel.InputsByDisplayOrder)
            {
                bool any = false;

                foreach (var connection in inputPortModel.ConnectionPortModels)
                {
                    any = true;
                    nodeModel.OnConnection(inputPortModel, connection);
                }

                if (!any)
                    nodeModel.OnConnection(inputPortModel, null);
            }

            foreach (var outputPortModel in nodeModel.OutputsByDisplayOrder)
            {
                bool any = false;

                foreach (var connection in outputPortModel.ConnectionPortModels)
                {
                    any = true;
                    nodeModel.OnConnection(outputPortModel, connection);
                }

                if (!any)
                    nodeModel.OnConnection(outputPortModel, null);
            }
        }
    }
}
