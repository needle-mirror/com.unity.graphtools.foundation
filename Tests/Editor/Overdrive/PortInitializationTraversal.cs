using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public class PortInitializationTraversal : GraphTraversal
    {
        public List<Action<IGTFNodeModel>> Callbacks = new List<Action<IGTFNodeModel>>();
        protected override void VisitNode(IGTFNodeModel nodeModel, HashSet<IGTFNodeModel> visitedNodes)
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

                var connectionPortModels = inputPortModel?.ConnectionPortModels ?? Enumerable.Empty<IGTFPortModel>();
                foreach (var connection in connectionPortModels)
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

                var connectionPortModels = outputPortModel?.ConnectionPortModels ?? Enumerable.Empty<IGTFPortModel>();
                foreach (var connection in connectionPortModels)
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
