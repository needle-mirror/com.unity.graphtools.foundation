using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    abstract class AutoPlacementHelper
    {
        protected GraphView m_GraphView;

        HashSet<IGTFGraphElementModel> m_SelectedElementModels = new HashSet<IGTFGraphElementModel>();
        HashSet<IGTFGraphElementModel> m_LeftOverElementModels;

        internal Dictionary<IGTFNodeModel, HashSet<IGTFNodeModel>> NodeDependencies { get; } = new Dictionary<IGTFNodeModel, HashSet<IGTFNodeModel>>(); // All parent nodes and their children nodes
        Dictionary<IGTFNodeModel, IGTFNodeModel> m_DependenciesToMerge = new Dictionary<IGTFNodeModel, IGTFNodeModel>();  // All parent nodes that will merge into another parent node

        protected void SendPlacementAction(List<IGTFGraphElementModel> updatedModels, List<Vector2> updatedDeltas)
        {
            var models = updatedModels.OfType<IPositioned>();
            m_GraphView.Store.Dispatch(new AutoPlaceElementsAction(updatedDeltas, models.ToArray()));
        }

        protected Dictionary<IGTFGraphElementModel, Vector2> GetElementDeltaResults()
        {
            GetSelectedElementModels();

            // Elements will be moved by a delta depending on their bounding rect
            List<Tuple<Rect, List<IGTFGraphElementModel>>> boundingRects = GetBoundingRects();

            return GetDeltas(boundingRects);
        }

        protected abstract void UpdateReferencePosition(ref float referencePosition, Rect currentElementRect);

        protected abstract Vector2 GetDelta(Rect elementPosition, float referencePosition);

        protected abstract float GetStartingPosition(List<Tuple<Rect, List<IGTFGraphElementModel>>> boundingRects);

        void GetSelectedElementModels()
        {
            m_SelectedElementModels.Clear();

            foreach (GraphElement element in m_GraphView.Selection.OfType<GraphElement>().Where(element => !(element is Edge)))
            {
                m_SelectedElementModels.Add(element.Model);
            }
            m_LeftOverElementModels = new HashSet<IGTFGraphElementModel>(m_SelectedElementModels);
        }

        List<Tuple<Rect, List<IGTFGraphElementModel>>> GetBoundingRects()
        {
            List<Tuple<Rect, List<IGTFGraphElementModel>>> boundingRects = new List<Tuple<Rect, List<IGTFGraphElementModel>>>();

            GetConnectedNodesBoundingRects(ref boundingRects);
            GetPlacematsBoundingRects(ref boundingRects);
            GetLeftOversBoundingRects(ref boundingRects);

            return boundingRects;
        }

        void GetConnectedNodesBoundingRects(ref List<Tuple<Rect, List<IGTFGraphElementModel>>> boundingRects)
        {
            ComputeNodeDependencies();
            boundingRects.AddRange(NodeDependencies.Keys.Select(parent =>
                new Tuple<Rect, List<IGTFGraphElementModel>>(GetConnectedNodesBoundingRect(NodeDependencies, parent), new List<IGTFGraphElementModel>(NodeDependencies[parent]))));
        }

        void GetPlacematsBoundingRects(ref List<Tuple<Rect, List<IGTFGraphElementModel>>> boundingRects)
        {
            List<IGTFPlacematModel> selectedPlacemats = m_SelectedElementModels.OfType<IGTFPlacematModel>().ToList();
            foreach (var placemat in selectedPlacemats.Where(placemat => m_LeftOverElementModels.Contains(placemat)))
            {
                Placemat placematUI = placemat.GetUI<Placemat>(m_GraphView);
                if (placematUI != null)
                {
                    var boundingRect = GetPlacematBoundingRect(ref boundingRects, placematUI, selectedPlacemats);
                    boundingRects.Add(new Tuple<Rect, List<IGTFGraphElementModel>>(boundingRect.Key, boundingRect.Value));
                }
            }
        }

        void GetLeftOversBoundingRects(ref List<Tuple<Rect, List<IGTFGraphElementModel>>> boundingRects)
        {
            foreach (IGTFGraphElementModel element in m_LeftOverElementModels)
            {
                GraphElement elementUI = element.GetUI(m_GraphView);
                if (elementUI != null)
                {
                    boundingRects.Add(new Tuple<Rect, List<IGTFGraphElementModel>>(elementUI.GetPosition(), new List<IGTFGraphElementModel> { element }));
                }
            }
        }

        Rect GetConnectedNodesBoundingRect(Dictionary<IGTFNodeModel, HashSet<IGTFNodeModel>> parentsWithChildren, IGTFNodeModel parent)
        {
            Rect boundingRect = Rect.zero;
            foreach (Node childUI in parentsWithChildren[parent].Select(child => child.GetUI<Node>(m_GraphView)).Where(childUI => childUI != null))
            {
                if (boundingRect == Rect.zero)
                {
                    boundingRect = childUI.GetPosition();
                }
                AdjustBoundingRect(ref boundingRect, childUI.GetPosition());
            }

            return boundingRect;
        }

        void ComputeNodeDependencies()
        {
            NodeDependencies.Clear();
            m_DependenciesToMerge.Clear();

            foreach (IGTFEdgeModel edgeModel in GetSortedSelectedEdgeModels())
            {
                IGTFNodeModel parentModel = edgeModel.ToPort.NodeModel;
                IGTFNodeModel childModel = edgeModel.FromPort.NodeModel;

                if (parentModel == childModel)
                {
                    // Node is its own parent
                    continue;
                }

                MergeNodeDependencies(parentModel, childModel);

                AddChildToParent(parentModel, childModel);
            }

            // Remove parents that were merged
            foreach (IGTFNodeModel parentToRemove in m_DependenciesToMerge.Keys)
            {
                NodeDependencies.Remove(parentToRemove);
            }
        }

        IEnumerable<IGTFEdgeModel> GetSortedSelectedEdgeModels()
        {
            return m_GraphView.Edges.ToList().Where(edge => m_SelectedElementModels.Contains(edge.Input.NodeModel) && m_SelectedElementModels.Contains(edge.Output.NodeModel))
                .Select(edge => edge.EdgeModel).OrderBy(edge => edge.ToPort.NodeModel.Position.x);
        }

        void MergeNodeDependencies(IGTFNodeModel currentParent, IGTFNodeModel currentChild)
        {
            // Current parent and current child have a child in common
            MergeSameOtherChild(currentParent, currentChild);

            // Current parent and any of its children have the current child in common
            MergeSameCurrentChild(currentParent, currentChild);

            // Current parent and current child have a parent in common
            MergeSameOtherParent(currentParent, currentChild);
        }

        void MergeSameOtherChild(IGTFNodeModel currentParent, IGTFNodeModel currentChild)
        {
            // Visual example
            // +---------------+                              +----------------+
            // | other child X o--+-----------------------+---o current parent |
            // +---------------+  |   +---------------+   |   +----------------+
            //                    +---o current child o---+
            //                    |   +---------------+
            // +---------------+  |
            // |               o--+
            // +---------------+

            if (NodeDependencies.TryGetValue(currentChild, out HashSet<IGTFNodeModel> otherChildren))
            {
                if (otherChildren.ToList().Any(otherChild => NodeDependencies.ContainsKey(currentParent) && NodeDependencies[currentParent].Contains(otherChild)))
                {
                    MergeDependencies(currentParent, currentChild);
                }
            }
        }

        void MergeSameCurrentChild(IGTFNodeModel currentParent, IGTFNodeModel currentChild)
        {
            // Visual example
            // +---------------+                              +----------------+
            // | current child o--+-----------------------+---o current parent |
            // +---------------+  |   +---------------+   |   +----------------+
            //                    +---o other child X o---+
            //                    |   +---------------+
            // +---------------+  |
            // |               o--+
            // +---------------+

            if (NodeDependencies.ContainsKey(currentParent))
            {
                foreach (IGTFNodeModel otherChild in NodeDependencies[currentParent].ToList())
                {
                    if (NodeDependencies.TryGetValue(otherChild, out HashSet<IGTFNodeModel> otherChildren) && otherChildren.Contains(currentChild))
                    {
                        // current parent and one of its children (X) are both parent to current child, merge current parent with X : X's children will all become current parent's children
                        MergeDependencies(currentParent, otherChild);
                    }
                }
            }
        }

        void MergeSameOtherParent(IGTFNodeModel currentParent, IGTFNodeModel currentChild)
        {
            // Visual example
            // +---------------+                              +----------------+
            // | current child o--+-----------------------+---o other parent X |
            // +---------------+  |   +---------------+   |   +----------------+
            //                    +---o currentparent o---+
            //                    |   +---------------+
            // +---------------+  |
            // |               o--+
            // +---------------+

            foreach (IGTFNodeModel otherParent in NodeDependencies.Keys.ToList().Where(otherParent =>
                NodeDependencies[otherParent].Contains(currentChild) && NodeDependencies[otherParent].Contains(currentParent)))
            {
                // current child and current parent both have the same parent (X), merge current parent with X : current parent's children will all become X's children
                MergeDependencies(otherParent, currentParent);
            }
        }

        void MergeDependencies(IGTFNodeModel parentToMergeInto, IGTFNodeModel parentToRemove)
        {
            if (NodeDependencies.ContainsKey(parentToMergeInto) && NodeDependencies.ContainsKey(parentToRemove))
            {
                NodeDependencies[parentToMergeInto].AddRange(NodeDependencies[parentToRemove]);
                m_DependenciesToMerge[parentToRemove] = parentToMergeInto;
            }
        }

        void AddChildToParent(IGTFNodeModel currentParent, IGTFNodeModel currentChild)
        {
            if (!NodeDependencies.ContainsKey(currentParent))
            {
                NodeDependencies.Add(currentParent, new HashSet<IGTFNodeModel>());
            }

            NodeDependencies[GetParent(currentParent)].Add(currentChild);
            m_LeftOverElementModels.Remove(currentChild);
        }

        IGTFNodeModel GetParent(IGTFNodeModel currentParent)
        {
            IGTFNodeModel newParent = currentParent;

            while (m_DependenciesToMerge.ContainsKey(newParent))
            {
                // Get the parent that the current dependency will be merged into
                newParent = m_DependenciesToMerge[currentParent];
            }

            return newParent;
        }

        KeyValuePair<Rect, List<IGTFGraphElementModel>> GetPlacematBoundingRect(ref List<Tuple<Rect, List<IGTFGraphElementModel>>> boundingRects, Placemat placematUI, List<IGTFPlacematModel> selectedPlacemats)
        {
            Rect boundingRect = placematUI.GetPosition();
            List<IGTFGraphElementModel> elementsOnBoundingRect = new List<IGTFGraphElementModel>();
            List<Placemat> placematsOnBoundingRect = GetPlacematsOnBoundingRect(ref boundingRect, ref elementsOnBoundingRect, selectedPlacemats);

            // Adjust the bounding rect with elements overlapping any of the placemats on the bounding rect
            AdjustPlacematBoundingRect(ref boundingRect, ref elementsOnBoundingRect, placematsOnBoundingRect);

            foreach (var otherRect in boundingRects.ToList())
            {
                Rect otherBoundingRect = otherRect.Item1;
                List<IGTFGraphElementModel> otherBoundingRectElements = otherRect.Item2;
                if (otherBoundingRectElements.Any(element => IsOnPlacemats(element.GetUI(m_GraphView), placematsOnBoundingRect)))
                {
                    AdjustBoundingRect(ref boundingRect, otherBoundingRect);
                    elementsOnBoundingRect.AddRange(otherBoundingRectElements);
                    boundingRects.Remove(otherRect);
                }
            }

            return new KeyValuePair<Rect, List<IGTFGraphElementModel>>(boundingRect, elementsOnBoundingRect);
        }

        protected virtual List<Tuple<Rect, List<IGTFGraphElementModel>>> GetBoundingRectsList(List<Tuple<Rect, List<IGTFGraphElementModel>>> boundingRects)
        {
            return boundingRects;
        }

        Dictionary<IGTFGraphElementModel, Vector2> GetDeltas(List<Tuple<Rect, List<IGTFGraphElementModel>>> boundingRects)
        {
            List<Tuple<Rect, List<IGTFGraphElementModel>>> boundingRectsList = GetBoundingRectsList(boundingRects);

            float referencePosition = GetStartingPosition(boundingRectsList);

            Dictionary<IGTFGraphElementModel, Vector2> deltas = new Dictionary<IGTFGraphElementModel, Vector2>();

            foreach (var(boundingRect, elements) in boundingRectsList)
            {
                Vector2 delta = GetDelta(boundingRect, referencePosition);
                foreach (var element in elements.Where(element => !deltas.ContainsKey(element)))
                {
                    deltas[element] = delta;
                }
                UpdateReferencePosition(ref referencePosition, boundingRect);
            }

            return deltas;
        }

        List<Placemat> GetPlacematsOnBoundingRect(ref Rect boundingRect, ref List<IGTFGraphElementModel> elementsOnBoundingRect, List<IGTFPlacematModel> selectedPlacemats)
        {
            List<Placemat> placematsOnBoundingRect = new List<Placemat>();

            foreach (IGTFPlacematModel placemat in selectedPlacemats.Where(placemat => m_LeftOverElementModels.Contains(placemat)))
            {
                Placemat placematUI = placemat.GetUI<Placemat>(m_GraphView);
                if (placematUI != null && placematUI.layout.Overlaps(boundingRect))
                {
                    AdjustBoundingRect(ref boundingRect, placematUI.GetPosition());

                    placematsOnBoundingRect.Add(placematUI);
                    elementsOnBoundingRect.Add(placemat);
                    m_LeftOverElementModels.Remove(placemat);
                }
            }

            return placematsOnBoundingRect;
        }

        void AdjustPlacematBoundingRect(ref Rect boundingRect, ref List<IGTFGraphElementModel> elementsOnBoundingRect, List<Placemat> placematsOnBoundingRect)
        {
            foreach (GraphElement elementUI in m_GraphView.GraphElements.ToList().Where(element => !(element is Placemat)))
            {
                if (elementUI != null && IsOnPlacemats(elementUI, placematsOnBoundingRect))
                {
                    AdjustBoundingRect(ref boundingRect, elementUI.GetPosition());
                    elementsOnBoundingRect.Add(elementUI.Model);
                    m_LeftOverElementModels.Remove(elementUI.Model);
                }
            }
        }

        static void AdjustBoundingRect(ref Rect boundingRect, Rect otherRect)
        {
            if (otherRect.yMin < boundingRect.yMin)
            {
                boundingRect.yMin = otherRect.yMin;
            }
            if (otherRect.xMin < boundingRect.xMin)
            {
                boundingRect.xMin = otherRect.xMin;
            }
            if (otherRect.yMax > boundingRect.yMax)
            {
                boundingRect.yMax = otherRect.yMax;
            }
            if (otherRect.xMax > boundingRect.xMax)
            {
                boundingRect.xMax = otherRect.xMax;
            }
        }

        static bool IsOnPlacemats(GraphElement element, List<Placemat> placemats)
        {
            return placemats.Any(placemat => !element.Equals(placemat) && element.layout.Overlaps(placemat.layout));
        }
    }
}
