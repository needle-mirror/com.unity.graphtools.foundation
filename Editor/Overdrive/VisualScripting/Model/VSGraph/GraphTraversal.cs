using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public abstract class GraphTraversal
    {
        public void VisitGraph(VSGraphModel vsGraphModel)
        {
            HashSet<INodeModel> visitedNodes = new HashSet<INodeModel>();
            foreach (var entryPoint in vsGraphModel.Stencil.GetEntryPoints(vsGraphModel))
            {
                VisitNode(entryPoint, visitedNodes);
            }

            // floating nodes
            foreach (var node in vsGraphModel.NodeModels)
            {
                if (node == null || visitedNodes.Contains(node))
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
                if (inputPortModel.IsConnected)
                    foreach (var connectionPortModel in inputPortModel.ConnectionPortModels)
                    {
                        if (!visitedNodes.Contains(connectionPortModel.NodeModel))
                            VisitNode(connectionPortModel.NodeModel, visitedNodes);
                    }
            }
        }

        protected virtual void VisitVariableDeclaration(IVariableDeclarationModel variableDeclarationModel) {}
    }
}
