using System;
using System.Collections.Generic;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Translators;
using UnityEngine;

namespace UnityEditor.VisualScripting.Model
{
    public abstract class GraphTraversal
    {
        public void VisitGraph(VSGraphModel vsGraphModel)
        {
            HashSet<IStackModel> visitedStacks = new HashSet<IStackModel>();
            HashSet<INodeModel> visitedNodes = new HashSet<INodeModel>();
            foreach (var entryPoint in vsGraphModel.Stencil.GetEntryPoints(vsGraphModel))
            {
                if (entryPoint is IStackModel entryStack)
                    VisitStack(entryStack, visitedStacks, visitedNodes);
                else
                    VisitNode(entryPoint, visitedNodes);
            }

            // floating stacks
            foreach (var stack in vsGraphModel.StackModels)
            {
                if (visitedStacks.Contains(stack))
                    continue;

                VisitStack(stack, visitedStacks, visitedNodes);
            }

            // floating nodes
            foreach (var node in vsGraphModel.NodeModels)
            {
                if (node == null || node is IStackModel || visitedNodes.Contains(node))
                    continue;

                VisitNode(node, visitedNodes);
            }

            foreach (var variableDeclaration in vsGraphModel.GraphVariableModels)
            {
                VisitVariableDeclaration(variableDeclaration);
            }

            foreach (var edgeModel in vsGraphModel.EdgeModels)
            {
                VisitEdge(edgeModel);
            }
        }

        protected virtual void VisitEdge(IEdgeModel edgeModel)
        {
        }

        protected virtual void VisitStack(IStackModel stack, HashSet<IStackModel> visitedStacks, HashSet<INodeModel> visitedNodes)
        {
            visitedStacks.Add(stack);

            // instance/data ports on stacks
            foreach (var inputPortModel in stack.InputPorts)
            {
                if (inputPortModel.PortType != PortType.Execution &&
                    inputPortModel.Connected)
                {
                    bool any = false;
                    foreach (var connectionPortModel in inputPortModel.ConnectionPortModels)
                    {
                        if (!visitedNodes.Contains(connectionPortModel.NodeModel))
                            VisitNode(connectionPortModel.NodeModel, visitedNodes);

                        any = true;
                        stack.OnConnection(inputPortModel, connectionPortModel);
                    }

                    if (!any)
                        stack.OnConnection(inputPortModel, null);
                }
            }

            // Still not visiting variable parameters...
            if (stack is IHasVariableDeclaration hasVariableDeclaration)
            {
                foreach (var variableDeclaration in hasVariableDeclaration.VariableDeclarations)
                {
                    VisitVariableDeclaration(variableDeclaration);
                }
            }

            foreach (INodeModel nodeModel in stack.NodeModels)
            {
                VisitNode(nodeModel, visitedNodes);
            }

            foreach (StackBaseModel connectedStack in RoslynTranslator.GetConnectedStacks(stack))
            {
                if (connectedStack == null || visitedStacks.Contains(connectedStack))
                    continue;
                VisitStack(connectedStack, visitedStacks, visitedNodes);
            }
        }

        protected virtual void VisitNode(INodeModel nodeModel, HashSet<INodeModel> visitedNodes)
        {
            if (nodeModel == null)
                return;

            visitedNodes.Add(nodeModel);

            if (nodeModel is IHasVariableDeclaration hasVariableDeclaration)
            {
                foreach (var variableDeclaration in hasVariableDeclaration.VariableDeclarations)
                {
                    VisitVariableDeclaration(variableDeclaration);
                }
            }

            foreach (var inputPortModel in nodeModel.InputsByDisplayOrder)
            {
                if (inputPortModel.Connected)
                    foreach (var connectionPortModel in inputPortModel.ConnectionPortModels)
                    {
                        if (!visitedNodes.Contains(connectionPortModel.NodeModel))
                            VisitNode(connectionPortModel.NodeModel, visitedNodes);
                    }
            }
        }

        protected virtual void VisitVariableDeclaration(IVariableDeclarationModel variableDeclarationModel) { }
    }
}
