using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    sealed class UIPartialRebuilder : IDisposable
    {
        int m_NumDeleted, m_NumCreated;

        HashSet<IGTFNodeModel> m_NodesToRebuild;
        HashSet<IEdgeModel> m_EdgesToRebuild;
        HashSet<IGTFEdgeModel> m_EdgesToDelete;
        HashSet<IGTFGraphElementModel> m_OtherElementsToRebuild;
        HashSet<GraphElement> m_GraphElementsToDelete;

        readonly Overdrive.State m_CurrentState;
        readonly Func<IGTFGraphElementModel, GraphElement> m_CreateElement;
        readonly Action<GraphElement> m_DeleteElement;

        public string DebugOutput => $"Graph UI: -{m_NumDeleted}/+{m_NumCreated} elements" + (BlackboardChanged ? " (+full blackboard)" : "");
        public bool AnyChangeMade => m_NumDeleted > 0 || m_NumCreated > 0;
        public bool BlackboardChanged { get; set; }

        public UIPartialRebuilder(Overdrive.State currentState, Func<IGTFGraphElementModel, GraphElement> createElement, Action<GraphElement> deleteElement)
        {
            m_NumDeleted = 0;
            m_NumCreated = 0;
            BlackboardChanged = false;

            m_NodesToRebuild = new HashSet<IGTFNodeModel>();
            m_EdgesToRebuild = new HashSet<IEdgeModel>();
            m_EdgesToDelete = new HashSet<IGTFEdgeModel>();
            m_OtherElementsToRebuild = new HashSet<IGTFGraphElementModel>();
            m_GraphElementsToDelete = new HashSet<GraphElement>();

            m_CurrentState = currentState;
            m_CreateElement = createElement;
            m_DeleteElement = deleteElement;
        }

        public void Dispose()
        {
            m_NodesToRebuild.Clear();
            m_EdgesToRebuild.Clear();
            m_EdgesToDelete.Clear();
            m_OtherElementsToRebuild.Clear();
            m_GraphElementsToDelete.Clear();
        }

        public void ComputeChanges(IGraphChangeList graphChangeList, Dictionary<IGTFGraphElementModel, GraphElement> existingElements)
        {
            BlackboardChanged = graphChangeList.BlackBoardChanged;

            GetChangesFromChangelist(graphChangeList);

            GatherDeletedElements(existingElements, graphChangeList);

            UpdateEdgesToRebuildFromNodesToRebuild();

            RemoveDeletedModelsFromRebuildLists(existingElements);

            MarkEdgesToBeRebuiltToDelete(existingElements);
        }

        void MarkEdgesToBeRebuiltToDelete(Dictionary<IGTFGraphElementModel, GraphElement> existingElements)
        {
            foreach (IEdgeModel edgeModel in m_EdgesToRebuild)
            {
                if (existingElements.ContainsKey(edgeModel))
                {
                    m_GraphElementsToDelete.Add(existingElements[edgeModel]);
                }
            }
        }

        void GetChangesFromChangelist(IGraphChangeList graphChanges)
        {
            foreach (var model in graphChanges.ChangedElements)
            {
                if (model is IEdgeModel edgeModel)
                {
                    if (graphChanges.DeletedEdges.Contains(edgeModel))
                        m_EdgesToDelete.Add(edgeModel);
                    else
                        m_EdgesToRebuild.Add(edgeModel);
                }
                else if (model is IGTFNodeModel nodeModel)
                {
                    m_NodesToRebuild.Add(nodeModel);
                    if (nodeModel is IGTFVariableNodeModel variableModel && variableModel.VariableDeclarationModel == null)
                    {
                        // In particular, ThisNodeModel sometimes requires an update of the blackboard
                        BlackboardChanged = true;
                    }
                }
                else if (model is IVariableDeclarationModel decl)
                {
                    BlackboardChanged = true;
                }
                else if (model is IGTFStickyNoteModel)
                {
                    m_OtherElementsToRebuild.Add(model);
                }
                else if (model is IGTFPlacematModel)
                {
                    m_OtherElementsToRebuild.Add(model);
                }
                else
                {
                    Debug.LogWarning($"Unexpected model to rebuild: {model.GetType().Name}, make sure it is correctly supported by UI Partial Rebuild.");
                    m_OtherElementsToRebuild.Add(model);
                }
            }
        }

        void GatherDeletedElements(Dictionary<IGTFGraphElementModel, GraphElement> existingElements, IGraphChangeList graphChangeList)
        {
            foreach (var elementModel in existingElements.Keys)
            {
                if ((elementModel as IDestroyable)?.Destroyed ?? false)
                {
                    var graphElement = existingElements[elementModel];
                    if (elementModel is IGTFNodeModel node)
                    {
                        m_EdgesToDelete.AddRange(node.GetConnectedEdges());
                    }
                    m_GraphElementsToDelete.Add(graphElement);
                }
            }

            foreach (var edge in graphChangeList.DeletedEdges)
            {
                if (existingElements.TryGetValue(edge, out GraphElement edgeGraphElement))
                    m_GraphElementsToDelete.Add(edgeGraphElement);
            }
        }

        public void DeleteEdgeModels()
        {
            var graphModel = m_CurrentState?.CurrentGraphModel;
            if (graphModel != null)
            {
                foreach (var edgeModel1 in m_EdgesToDelete)
                {
                    var edgeModel = (EdgeModel)edgeModel1;
                    graphModel.DeleteEdge(edgeModel);
                }
            }
        }

        void UpdateEdgesToRebuildFromNodesToRebuild()
        {
            foreach (IGTFNodeModel nodeModel in m_NodesToRebuild)
            {
                if (!nodeModel.Destroyed)
                {
                    foreach (var gtfEdgeModel in nodeModel.GetConnectedEdges())
                    {
                        var edgeModel = (IEdgeModel)gtfEdgeModel;
                        m_EdgesToRebuild.Add(edgeModel);
                    }
                }
            }
        }

        void RemoveDeletedModelsFromRebuildLists(Dictionary<IGTFGraphElementModel, GraphElement> existingElements)
        {
            m_EdgesToRebuild.RemoveWhere(e => existingElements.ContainsKey(e) && m_GraphElementsToDelete.Contains(existingElements[e]));
            m_NodesToRebuild.RemoveWhere(n => existingElements.ContainsKey(n) && m_GraphElementsToDelete.Contains(existingElements[n]));
        }

        public void DeleteGraphElements()
        {
            foreach (GraphElement graphElement in m_GraphElementsToDelete)
            {
                m_DeleteElement(graphElement);
                m_NumDeleted++;
            }
        }

        public void RebuildNodes(Dictionary<IGTFGraphElementModel, GraphElement> existingElements)
        {
            foreach (var nodeModel in m_NodesToRebuild)
            {
                // delete node if existing
                if (existingElements.TryGetValue(nodeModel, out var oldElement))
                {
                    m_DeleteElement(oldElement);
                    m_NumDeleted++;
                }

                // create node
                GraphElement element = m_CreateElement(nodeModel);
                existingElements[nodeModel] = element;
                if (element != null)
                {
                    m_NumCreated++;
                }
            }

            foreach (var elementModel in m_OtherElementsToRebuild)
            {
                // delete node if existing
                if (existingElements.TryGetValue(elementModel, out var oldElement))
                {
                    m_DeleteElement(oldElement);
                    m_NumDeleted++;
                }

                // create node
                if (m_CreateElement(elementModel) != null)
                {
                    m_NumCreated++;
                }
            }
        }

        public void RebuildEdges(Action<IEdgeModel> rebuildEdge)
        {
            foreach (IEdgeModel edgeModel in m_EdgesToRebuild)
            {
                rebuildEdge(edgeModel);
                m_NumCreated++;
            }
        }
    }
}
