using System;
using System.Collections.Generic;
using UnityEditor.VisualScripting.GraphViewModel;

namespace UnityEditor.VisualScripting.Model
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

        protected override void VisitStack(IStackModel stack, HashSet<IStackModel> visitedStacks, HashSet<INodeModel> visitedNodes)
        {
            base.VisitStack(stack, visitedStacks, visitedNodes);

            IFunctionModel owner = null;
            foreach (var stackInputPortModel in stack.InputPorts)
            {
                if (!stackInputPortModel.Connected)
                    continue;
                foreach (var connectionPortModel in stackInputPortModel.ConnectionPortModels)
                {
                    if (connectionPortModel.NodeModel is StackBaseModel stackModel)
                        owner = stackModel.OwningFunctionModel;
                    else
                        owner = connectionPortModel.NodeModel.ParentStackModel?.OwningFunctionModel;

                    if (owner != null)
                        break;
                }

                if (owner != null)
                    break;
            }

            ((StackBaseModel)stack).OwningFunctionModel = owner;

            foreach (var callback in Callbacks)
                callback(stack);
        }
    }
}
