using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.SmartSearch;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Profiling;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public sealed class VseUIController
    {
        readonly VseGraphView m_GraphView;
        readonly Store m_Store;

        // PF: Remove and use GetUI
        public Dictionary<IGTFGraphElementModel, GraphElement> ModelsToNodeMapping { get; private set; }

        public Blackboard Blackboard { get; }

        readonly VisualElement m_IconsParent;
        IGTFGraphModel m_LastGraphModel;
        public IGTFGraphModel LastGraphModel => m_LastGraphModel;

        public VseUIController(VseGraphView graphView, Store store)
        {
            m_GraphView = graphView;
            m_Store = store;
            m_IconsParent = new VisualElement { name = "iconsParent"};
            m_IconsParent.style.overflow = Overflow.Visible;
            Blackboard = new Blackboard(m_Store, m_GraphView, windowed: true);
            m_LastGraphModel = null;
        }

        internal void ClearCompilationErrors()
        {
            m_GraphView.Query().Descendents<IconBadge>().ForEach(badge =>
            {
                badge.Detach();
                badge.RemoveFromHierarchy();
            });
        }

        internal void UpdateViewTransform(FindInGraphAdapter.FindSearcherItem item)
        {
            if (item == null)
                return;

            GraphElement elt = null;
            Vector3 frameTranslation;
            Vector3 frameScaling;

            if (ModelsToNodeMapping?.TryGetValue(item.Node, out elt) ?? false)
            {
                m_GraphView.ClearSelection();
                m_GraphView.AddToSelection(elt);

                Rect rect = elt.parent.ChangeCoordinatesTo(m_GraphView.contentViewContainer, elt.localBound);
                GraphView.CalculateFrameTransform(
                    rect, m_GraphView.layout, 30, out frameTranslation, out frameScaling
                );
            }
            else
            {
                Debug.LogError("no ui mapping for " + item.Name);
                GraphView.CalculateFrameTransform(
                    new Rect(item.Node.Position, Vector2.one),
                    m_GraphView.layout,
                    30,
                    out frameTranslation,
                    out frameScaling
                );
            }

            m_GraphView.UpdateViewTransform(frameTranslation, frameScaling);
        }

        internal void UpdateTopology()
        {
            Profiler.BeginSample("UpdateTopology");
            Stopwatch topologyStopwatch = new Stopwatch();
            topologyStopwatch.Start();

            var state = m_Store.GetState();
            var currentGraphModel = state.CurrentGraphModel;
            if (currentGraphModel == null)
            {
                return;
            }

            GraphChangeList graphChangeList = currentGraphModel.LastChanges;
            string dispatchedActionName = state.LastDispatchedActionName; // save this now, because some actions trigger a UIRefresh, hiding the original action (TODO)

            m_GraphView.DisablePersistedSelectionRestore();

            bool fullUIRebuildOnChange = state.Preferences.GetBool(BoolPref.FullUIRebuildOnChange);
            bool forceRebuildUI = fullUIRebuildOnChange || currentGraphModel != m_LastGraphModel || graphChangeList == null || !graphChangeList.HasAnyTopologyChange() || ModelsToNodeMapping == null;

            if (forceRebuildUI) // no specific graph changes passed, assume rebuild everything
            {
                RebuildAll(state);
            }
            else
            {
                PartialRebuild(state);
            }

            state.EditorDataModel.ClearElementsToSelectUponCreation();

            MapModelsToNodes();

            if (state.Preferences.GetBool(BoolPref.ShowUnusedNodes))
                m_GraphView.PositionDependenciesManagers.UpdateNodeState();

            m_GraphView.HighlightGraphElements();

            m_LastGraphModel = currentGraphModel;

            m_GraphView.EnablePersistedSelectionRestore();

            if (ElementToRename != null)
            {
                m_GraphView.ClearSelection();
                m_GraphView.AddToSelection((GraphElement)ElementToRename);
                ElementToRename.Rename(forceRename: true);
                ElementToRename = null;
                state.EditorDataModel.ElementModelToRename = null;
            }

            // We need to do this after all graph elements are created.
            foreach (var p in m_GraphView.PlacematContainer.Placemats)
            {
                p.UpdateFromModel();
            }

            m_GraphView.MarkDirtyRepaint();

            topologyStopwatch.Stop();
            Profiler.EndSample();

            if (state.Preferences.GetBool(BoolPref.WarnOnUIFullRebuild) && state.LastActionUIRebuildType == Overdrive.State.UIRebuildType.Full)
            {
                Debug.LogWarning($"Rebuilding the whole UI ({dispatchedActionName})");
            }
            if (state.Preferences.GetBool(BoolPref.LogUIBuildTime))
            {
                Debug.Log($"UI Update ({dispatchedActionName}) took {topologyStopwatch.ElapsedMilliseconds} ms");
            }
        }

        GraphElement CreateElement(IGTFGraphElementModel model)
        {
            GraphElement element = model.CreateUI<GraphElement>(m_GraphView, m_Store);
            if (element != null)
                AddToGraphView(element);

            return element;
        }

        void PartialRebuild(Overdrive.State state)
        {
            state.LastActionUIRebuildType = Overdrive.State.UIRebuildType.Partial;

            using (var partialRebuilder = new UIPartialRebuilder(state, CreateElement, RemoveFromGraphView))
            {
                // get changes into sensible lists (sets)
                partialRebuilder.ComputeChanges(state.CurrentGraphModel.LastChanges, ModelsToNodeMapping);

                // actually delete stuff
                partialRebuilder.DeleteEdgeModels();
                partialRebuilder.DeleteGraphElements();

                // update model to graphview mapping
                MapModelsToNodes();

                // rebuild nodes
                partialRebuilder.RebuildNodes(ModelsToNodeMapping);

                // rebuild needed edges
                partialRebuilder.RebuildEdges(e => RestoreEdge(e));

                if (partialRebuilder.BlackboardChanged)
                {
                    Blackboard?.Rebuild(GraphElements.Blackboard.RebuildMode.BlackboardOnly);
                }

                if (state.Preferences.GetBool(BoolPref.LogUIBuildTime))
                {
                    Debug.Log(partialRebuilder.DebugOutput);
                }
            }
        }

        internal void RemoveFromGraphView(GraphElement graphElement)
        {
            switch (graphElement)
            {
                case Edge e:
                    m_GraphView.RemovePositionDependency(e.EdgeModel);
                    break;
                case Node n when n.NodeModel is IGTFEdgePortalModel portalModel:
                    m_GraphView.RemovePortalDependency(portalModel);
                    break;
            }

            graphElement.Unselect(m_GraphView);
            m_GraphView.DeleteElements(new[] { graphElement });
            graphElement.UnregisterCallback<MouseOverEvent>(m_GraphView.OnMouseOver);
        }

        void RebuildAll(Overdrive.State state)
        {
            state.LastActionUIRebuildType = Overdrive.State.UIRebuildType.Full;
            Clear();

            var graphModel = state.CurrentGraphModel;
            if (graphModel == null)
                return;

            m_GraphView.PlacematContainer.RemoveAllPlacemats();
            foreach (var placematModel in state.CurrentGraphModel.PlacematModels.OrderBy(e => e.ZOrder))
            {
                var placemat = GraphElementFactory.CreateUI<GraphElement>(m_GraphView, m_Store, placematModel);
                if (placemat != null)
                    AddToGraphView(placemat);
            }

            foreach (var nodeModel in graphModel.NodeModels)
            {
                var node = GraphElementFactory.CreateUI<GraphElement>(m_GraphView, m_Store, nodeModel);
                if (node != null)
                    AddToGraphView(node);
            }

            foreach (var stickyNoteModel in state.CurrentGraphModel.StickyNoteModels)
            {
                var stickyNote = GraphElementFactory.CreateUI<GraphElement>(m_GraphView, m_Store, stickyNoteModel);
                if (stickyNote != null)
                    AddToGraphView(stickyNote);
            }

            MapModelsToNodes();

            int index = 0;
            foreach (var edge in state.CurrentGraphModel.EdgeModels)
            {
                if (!RestoreEdge(edge))
                {
                    Debug.LogWarning($"Edge {index} cannot be restored: {edge}");
                }
                index++;
            }

            Blackboard?.Rebuild(GraphElements.Blackboard.RebuildMode.BlackboardOnly);

            m_GraphView.contentViewContainer.Add(m_IconsParent);
            m_GraphView.HighlightGraphElements();
        }

        void MapModelsToNodes()
        {
            var hasGraphElementModels = m_GraphView.Query<GraphElement>()
                .Where(x => !(x is BlackboardVariableField) && x.Model != null)
                .ToList();

            ModelsToNodeMapping = hasGraphElementModels
                .GroupBy(x => x.Model, x => x)
                .ToDictionary(g => g.Key, g => g.First());
        }

        bool RestoreEdge(IGTFEdgeModel edge)
        {
            var inputPortModel = edge.ToPort;
            var outputPortModel = edge.FromPort;
            if (inputPortModel != null && outputPortModel != null)
            {
                Connect(edge);
                return true;
            }

            if (edge is EdgeModel e)
            {
                if (e.TryMigratePorts())
                {
                    Connect(edge);
                    return true;
                }

                // missing ports still displayed
                if (e.AddPlaceHolderPorts(out var inputNode, out var outputNode))
                {
                    if (inputNode != null && ModelsToNodeMapping[inputNode] is GraphElement inputNodeUi)
                        inputNodeUi.UpdateFromModel();
                    if (outputNode != null && ModelsToNodeMapping[outputNode] is GraphElement outputNodeUi)
                        outputNodeUi.UpdateFromModel();
                    Connect(edge);
                    return true;
                }
            }

            return false;
        }

        public void DisplayCompilationErrors(Overdrive.State state)
        {
            VseUtility.RemoveLogEntries();
            if (ModelsToNodeMapping == null)
                UpdateTopology();

            var lastCompilationResult = state.CompilationResultModel.GetLastResult();
            if (lastCompilationResult?.errors == null)
                return;

            var graphAsset = (GraphAssetModel)m_Store.GetState().CurrentGraphModel?.AssetModel;
            foreach (var error in lastCompilationResult.errors)
            {
                if (error.sourceNode != null && !error.sourceNode.Destroyed)
                {
                    var alignment = SpriteAlignment.RightCenter;

                    var graphElement = error.sourceNode.GetUI(m_GraphView);
                    if (graphElement != null)
                        AttachErrorBadge(graphElement, error.description, alignment, m_Store, error.quickFix);
                }

                if (graphAsset)
                {
                    var graphAssetPath = graphAsset ? AssetDatabase.GetAssetPath(graphAsset) : "<unknown>";
                    VseUtility.LogSticky(error.isWarning ? LogType.Warning : LogType.Error, LogOption.None,
                        $"{graphAssetPath}: {error.description}", $"{graphAssetPath}@{error.sourceNodeGuid}",
                        graphAsset.GetInstanceID());
                }
            }
        }

        void Connect(IGTFEdgeModel edgeModel)
        {
            var edge = GraphElementFactory.CreateUI<Edge>(m_GraphView, m_Store, edgeModel);
            AddToGraphView(edge);

            m_GraphView.AddPositionDependency(edge.EdgeModel);
        }

        internal void Clear()
        {
            List<GraphElement> elements = m_GraphView.GraphElements.ToList();

            m_GraphView.PositionDependenciesManagers.Clear();
            foreach (var element in elements)
            {
                m_GraphView.RemoveElement(element);

                element.UnregisterCallback<MouseOverEvent>(m_GraphView.OnMouseOver);
            }

            if (m_GraphView.contentViewContainer.Contains(m_IconsParent))
                m_GraphView.contentViewContainer.Remove(m_IconsParent);
        }

        internal void ResetBlackboard()
        {
            Blackboard.ClearContents();
            Blackboard.Clear();
        }

        internal void AttachValue(IBadgeContainer badgeContainer, VisualElement visualElement, string value, Color badgeColor, SpriteAlignment alignment)
        {
            Assert.IsNotNull(visualElement);

            badgeContainer.ShowValueBadge(m_IconsParent, visualElement, alignment, value, badgeColor);
        }

        internal static void ClearPorts(Node node)
        {
            node.Query<Port>().ForEach(p =>
            {
                ClearValue(p);
                p.ExecutionPortActive = false;
            });
        }

        internal static void ClearValue(VisualElement visualElement)
        {
            Assert.IsNotNull(visualElement);

            (visualElement as IBadgeContainer)?.HideValueBadge();
        }

        internal void AttachErrorBadge(VisualElement visualElement, string errorDescription, SpriteAlignment alignment, Store store = null, CompilerQuickFix errorQuickFix = null)
        {
            Assert.IsNotNull(visualElement);
            if (errorQuickFix != null)
                Assert.IsNotNull(store);

            VisualElement parent = visualElement.parent;
            while (parent.GetType().IsSubclassOf(typeof(GraphElement)) && parent.parent != null)
            {
                parent = parent.parent;
            }

            (visualElement as IBadgeContainer)?.ShowErrorBadge(m_IconsParent, alignment, errorDescription, store, errorQuickFix);
        }

        internal static void ClearErrorBadge(VisualElement visualElement)
        {
            Assert.IsNotNull(visualElement);

            (visualElement as IBadgeContainer)?.HideErrorBadge();
        }

        void AddToGraphView(GraphElement graphElement)
        {
            // exception thrown by graphview while in playmode
            // probably related to the undo selection thingy
            try
            {
                if (graphElement.parent == null) // Some elements (e.g. Placemats) come in already added to the right spot.
                    m_GraphView.AddElement(graphElement);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            if (graphElement != null &&
                m_Store.GetState().EditorDataModel.ShouldSelectElementUponCreation(graphElement.Model))
                graphElement.Select(m_GraphView, true);

            if (graphElement is Node || graphElement is Token || graphElement is Edge)
                graphElement.RegisterCallback<MouseOverEvent>(m_GraphView.OnMouseOver);

            if (graphElement?.Model is IGTFEdgePortalModel portalModel)
            {
                m_GraphView.AddPortalDependency(portalModel);
            }
        }

        public IRenamable ElementToRename { private get; set; }
    }
}
