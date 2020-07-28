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

            if (!(nodeModel is IInOutPortsNode node))
                return;

            foreach (var callback in Callbacks)
                callback(nodeModel);

            // do after left recursion, so the leftmost node is processed first
            foreach (var inputPortModel in node.InputsByDisplayOrder)
            {
                bool any = false;

                var connectionPortModels = inputPortModel?.GetConnectedPorts() ?? Enumerable.Empty<IGTFPortModel>();
                foreach (var connection in connectionPortModels)
                {
                    any = true;
                    node.OnConnection(inputPortModel, connection);
                }

                if (!any)
                    node.OnConnection(inputPortModel, null);
            }

            foreach (var outputPortModel in node.OutputsByDisplayOrder)
            {
                bool any = false;

                var connectionPortModels = outputPortModel?.GetConnectedPorts() ?? Enumerable.Empty<IGTFPortModel>();
                foreach (var connection in connectionPortModels)
                {
                    any = true;
                    node.OnConnection(outputPortModel, connection);
                }

                if (!any)
                    node.OnConnection(outputPortModel, null);
            }
        }
    }
}
