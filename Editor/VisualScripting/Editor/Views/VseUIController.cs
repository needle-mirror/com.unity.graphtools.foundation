using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.Editor.Highlighting;
using UnityEditor.VisualScripting.Editor.Renamable;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Profiling;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

namespace UnityEditor.VisualScripting.Editor
{
    public sealed class VseUIController
    {
        readonly VseGraphView m_GraphView;
        readonly Store m_Store;

        public Dictionary<IGraphElementModel, GraphElement> ModelsToNodeMapping { get; private set; }

        public Blackboard Blackboard { get; }

        readonly VisualElement m_IconsParent;
        IGraphModel m_LastGraphModel;
        public IGraphModel LastGraphModel => m_LastGraphModel;

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
                    new Rect(item.Node.ParentStackModel?.Position ?? item.Node.Position, Vector2.one),
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

            State state = m_Store.GetState();
            IGraphModel currentGraphModel = state.CurrentGraphModel;
            if (currentGraphModel == null)
            {
                return;
            }

            IGraphChangeList graphChangeList = currentGraphModel.LastChanges;
            string dispatchedActionName = state.LastDispatchedActionName; // save this now, because some actions trigger a UIRefresh, hiding the original action (TODO)

            m_GraphView.DisablePersistedSelectionRestore();

            bool fullUIRebuildOnChange = state.Preferences.GetBool(VSPreferences.BoolPref.FullUIRebuildOnChange);
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

            if (state.Preferences.GetBool(VSPreferences.BoolPref.ShowUnusedNodes))
                m_GraphView.PositionDependenciesManagers.UpdateNodeState(ModelsToNodeMapping);

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

#if UNITY_2020_1_OR_NEWER
            // We need to do this after all graph elements are created.
            foreach (var p in m_GraphView.placematContainer.Placemats.Cast<Placemat>())
            {
                p.InitCollapsedElementsFromModel();
            }
#endif

            m_GraphView.MarkDirtyRepaint();

            topologyStopwatch.Stop();
            Profiler.EndSample();

            if (state.Preferences.GetBool(VSPreferences.BoolPref.WarnOnUIFullRebuild) && state.lastActionUIRebuildType == State.UIRebuildType.Full)
            {
                Debug.LogWarning($"Rebuilding the whole UI ({dispatchedActionName})");
            }
            if (state.Preferences.GetBool(VSPreferences.BoolPref.LogUIBuildTime))
            {
                Debug.Log($"UI Update ({dispatchedActionName}) took {topologyStopwatch.ElapsedMilliseconds} ms");
            }
        }

        GraphElement CreateElement(IGraphElementModel model)
        {
            GraphElement element = GraphElementFactory.CreateUI(m_GraphView, m_Store, model);
            if (element != null)
                AddToGraphView(element);

            return element;
        }

        void PartialRebuild(State state)
        {
            state.lastActionUIRebuildType = State.UIRebuildType.Partial;

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
                // TODO keep a port mapping next to the node mapping
                Dictionary<PortModel, Experimental.GraphView.Port> allPorts = m_GraphView.ports.ToList().ToDictionary(p => p.userData as PortModel);
                m_GraphView.PositionDependenciesManagers.portModelToPort = allPorts;

                partialRebuilder.RebuildEdges(e => RestoreEdge(e, allPorts));

                if (partialRebuilder.BlackboardChanged)
                {
                    Blackboard?.Rebuild(Blackboard.RebuildMode.BlackboardOnly);
                }

                if (state.Preferences.GetBool(VSPreferences.BoolPref.LogUIBuildTime))
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
                    m_GraphView.RemovePositionDependency(e.model);
                    break;
                case Node n when n.Stack != null:
                    m_GraphView.RemovePositionDependencies(n.Stack.stackModel.Guid, new[] {n.model});
                    break;
            }

            m_GraphView.DeleteElements(new[] { graphElement });
            graphElement.UnregisterCallback<MouseOverEvent>(m_GraphView.OnMouseOver);
        }

        void RebuildAll(State state)
        {
            state.lastActionUIRebuildType = State.UIRebuildType.Full;
            Clear();

            var graphModel = state.CurrentGraphModel;
            if (graphModel == null)
                return;

#if UNITY_2020_1_OR_NEWER
            m_GraphView.placematContainer.RemoveAllPlacemats();
            foreach (var placematModel in ((VSGraphModel)state.CurrentGraphModel).PlacematModels.OrderBy(e => e.ZOrder))
            {
                var placemat = GraphElementFactory.CreateUI(m_GraphView, m_Store, placematModel);
                if (placemat != null)
                    AddToGraphView(placemat);
            }
#endif

            foreach (var nodeModel in graphModel.NodeModels)
            {
                var node = GraphElementFactory.CreateUI(m_GraphView, m_Store, nodeModel);
                if (node != null)
                    AddToGraphView(node);
            }

            foreach (var stickyNoteModel in ((VSGraphModel)state.CurrentGraphModel).StickyNoteModels)
            {
                var stickyNote = GraphElementFactory.CreateUI(m_GraphView, m_Store, stickyNoteModel);
                if (stickyNote != null)
                    AddToGraphView(stickyNote);
            }

            var ports = m_GraphView.ports.ToList();
            var allPorts = ports.ToDictionary(p => p.userData as PortModel);
            m_GraphView.PositionDependenciesManagers.portModelToPort = allPorts;
            int index = 0;
            foreach (IEdgeModel edge in ((VSGraphModel)state.CurrentGraphModel).EdgeModels)
            {
                if (!RestoreEdge(edge, allPorts))
                {
                    Debug.LogWarning($"Edge {index} cannot be restored: {edge}");
                }
                index++;
            }

            MapModelsToNodes();

            Blackboard?.Rebuild(Blackboard.RebuildMode.BlackboardOnly);

            m_GraphView.contentViewContainer.Add(m_IconsParent);
            m_GraphView.HighlightGraphElements();
        }

        void MapModelsToNodes()
        {
            var hasGraphElementModels = m_GraphView.Query<GraphElement>()
                .Where(x =>
                {
                    var hasGraphElementModel = x as IHasGraphElementModel;
                    return !(hasGraphElementModel is BlackboardVariableField) && hasGraphElementModel?.GraphElementModel != null;
                })
                .ToList();

            ModelsToNodeMapping = hasGraphElementModels
                .GroupBy(x => ((IHasGraphElementModel)x).GraphElementModel, x => x)
                .ToDictionary(g => g.Key, g => g.First());
        }

        bool RestoreEdge(IEdgeModel edge, Dictionary<PortModel, Experimental.GraphView.Port> portsByModel)
        {
            var inputPortModel = (PortModel)edge.InputPortModel;
            var outputPortModel = (PortModel)edge.OutputPortModel;
            if (inputPortModel != null && outputPortModel != null
                && portsByModel.TryGetValue(inputPortModel, out var i)
                && portsByModel.TryGetValue(outputPortModel, out var o))
            {
                Connect(edge, o, i);
                return true;
            }

            return false;
        }

        public void DisplayCompilationErrors(State state)
        {
            VseUtility.RemoveLogEntries();
            if (ModelsToNodeMapping == null)
                UpdateTopology();

            var lastCompilationResult = state.CompilationResultModel.GetLastResult();
            if (lastCompilationResult?.errors == null)
                return;

            foreach (var error in lastCompilationResult.errors)
            {
                if (error.sourceNode != null && !error.sourceNode.Destroyed)
                {
                    var alignment = error.sourceNode is IStackModel
                        ? SpriteAlignment.TopCenter
                        : SpriteAlignment.RightCenter;

                    ModelsToNodeMapping.TryGetValue(error.sourceNode, out var graphElement);
                    if (graphElement != null)
                        AttachErrorBadge(graphElement, error.description, alignment, m_Store, error.quickFix);
                }

                var graphAsset = (GraphAssetModel)m_Store.GetState().CurrentGraphModel?.AssetModel;
                var graphAssetPath = graphAsset ? AssetDatabase.GetAssetPath(graphAsset) : "<unknown>";
                VseUtility.LogSticky(error.isWarning ? LogType.Warning : LogType.Error, LogOption.None, $"{graphAssetPath}: {error.description}", $"{graphAssetPath}@{error.sourceNodeGuid}", graphAsset.GetInstanceID());
            }
        }

        void Connect(IEdgeModel edgeModel, Experimental.GraphView.Port output, Experimental.GraphView.Port input)
        {
            var edge = new Edge(edgeModel);
            AddToGraphView(edge);

            edge.input = input;
            edge.output = output;

            edge.input.Connect(edge);
            edge.output.Connect(edge);

            m_GraphView.AddPositionDependency(edge.model);
        }

        internal void Clear()
        {
            List<GraphElement> elements = m_GraphView.graphElements.ToList();

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

        internal void AttachValue(IBadgeContainer badgeContainer, VisualElement visualElement, string value, Color portPos, SpriteAlignment alignment)
        {
            Assert.IsNotNull(visualElement);

            badgeContainer.ShowValueBadge(m_IconsParent, visualElement, alignment, value, portPos);
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

        void AddToGraphView(GraphElement graphElement, Experimental.GraphView.Scope scope = null)
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

            scope?.AddElement(graphElement);

            if (graphElement is IHasGraphElementModel hasGraphElementModel &&
                m_Store.GetState().EditorDataModel.ShouldSelectElementUponCreation(hasGraphElementModel))
                graphElement.Select(m_GraphView, true);

            // Execute any extra stuff when element is added
            (graphElement as IVSGraphViewObserver)?.OnAddedToGraphView();

            if (graphElement is StackNode || graphElement is Node || graphElement is Token || graphElement is Edge)
                graphElement.RegisterCallback<MouseOverEvent>(m_GraphView.OnMouseOver);

            if (!(graphElement is Token token))
                return;

            IGraphElementModel elementModelToRename = m_Store.GetState().EditorDataModel.ElementModelToRename;
            if (elementModelToRename != null && ReferenceEquals(token.GraphElementModel, elementModelToRename))
                m_GraphView.UIController.ElementToRename = token;
        }

        public IRenamable ElementToRename { private get; set; }

        static void VisitNodes(Experimental.GraphView.Node node, ref HashSet<Experimental.GraphView.Node> visitedGraphElements)
        {
            if (node == null || visitedGraphElements == null || visitedGraphElements.Contains(node))
                return;

            if (node is StackNode)
            {
                foreach (var visualElement in node.inputContainer.Children())
                {
                    var port = (Port)visualElement;
                    if (port?.connections == null)
                        continue;

                    foreach (Experimental.GraphView.Edge connection in port.connections)
                    {
                        Experimental.GraphView.Port otherConnection = connection.output;
                        Experimental.GraphView.Node otherNode = otherConnection?.node;
                        if (otherNode == null)
                            continue;

                        visitedGraphElements.Add(otherNode);
                        VisitNodes(otherNode, ref visitedGraphElements);
                    }
                }
            }
            else
            {
                foreach (var visualElement in node.outputContainer.Children())
                {
                    var port = (Port)visualElement;
                    if (port?.connections == null)
                        continue;

                    foreach (Experimental.GraphView.Edge connection in port.connections)
                    {
                        Experimental.GraphView.Port otherConnection = connection.input;
                        Experimental.GraphView.Node otherNode = otherConnection?.node;
                        if (otherNode == null)
                            continue;

                        visitedGraphElements.Add(otherNode);
                        VisitNodes(otherNode, ref visitedGraphElements);
                    }
                }
            }

            switch (node)
            {
                case FunctionNode functionNode:
                    visitedGraphElements.Add(functionNode);
                    break;
                case Node vsNode when vsNode.IsInStack:
                    visitedGraphElements.Add(vsNode.Stack);
                    break;
            }
        }

        static void FetchVariableDeclarationModelsFromFunctionModel(IFunctionModel functionModel, ref HashSet<Tuple<IVariableDeclarationModel, bool>> declarationModelTuples)
        {
            if (functionModel.FunctionVariableModels != null)
                foreach (var declarationModel in functionModel.FunctionVariableModels)
                    declarationModelTuples.Add(new Tuple<IVariableDeclarationModel, bool>(declarationModel, true));

            IEnumerable<IVariableDeclarationModel> allFunctionParameterModels = functionModel.FunctionParameterModels;
            if (allFunctionParameterModels == null)
                return;

            foreach (var declarationModel in allFunctionParameterModels)
                declarationModelTuples.Add(new Tuple<IVariableDeclarationModel, bool>(declarationModel,
                    functionModel.AllowChangesToModel));
        }

        void FindVariableDeclarationsFromSelection(List<ISelectable> selection, HashSet<INodeModel> visited, HashSet<Tuple<IVariableDeclarationModel, bool>> declarationModelTuples)
        {
            foreach (ISelectable x in selection)
            {
                switch (x)
                {
                    case Token token:
                    {
                        var declarationModel = (token.GraphElementModel as IVariableModel)?.DeclarationModel as VariableDeclarationModel;
                        if (declarationModel != null && declarationModel.FunctionModel != null)
                            FetchVariableDeclarationModelsFromFunctionModel(declarationModel.FunctionModel, ref declarationModelTuples);
                    }
                    break;
                    case TokenDeclaration tokenDeclaration when tokenDeclaration.Declaration is VariableDeclarationModel declarationModel &&
                        declarationModel.FunctionModel != null:
                        FetchVariableDeclarationModelsFromFunctionModel(declarationModel.FunctionModel, ref declarationModelTuples);
                        break;
                    case StackNode stackNode:
                    {
                        if (stackNode is FunctionNode functionNode)
                        {
                            FetchVariableDeclarationModelsFromFunctionModel(functionNode.m_FunctionModel, ref declarationModelTuples);
                        }
                        else
                        {
                            List<ISelectable> connectedInputNodes = new List<ISelectable>();
                            foreach (var inputPortModel in stackNode.stackModel.InputsById.Values)
                                foreach (var connectedPortModel in inputPortModel.ConnectionPortModels)
                                    if (visited.Add(connectedPortModel.NodeModel) && connectedPortModel.NodeModel != stackNode.stackModel)
                                        connectedInputNodes.Add(ModelsToNodeMapping[connectedPortModel.NodeModel]);
                            FindVariableDeclarationsFromSelection(connectedInputNodes, visited, declarationModelTuples);
                        }
                    }
                    break;
                    case Node node:
                        if (node.Stack != null && visited.Add(node.Stack.stackModel))
                        {
                            if (node.Stack is FunctionNode parentFunctionNode)
                                FetchVariableDeclarationModelsFromFunctionModel(parentFunctionNode.m_FunctionModel, ref declarationModelTuples);
                            else
                                FindVariableDeclarationsFromSelection(new List<ISelectable> { node.Stack }, visited, declarationModelTuples);
                        }

                        break;
                    case BlackboardVariableField blackboardVariableField:
                    {
                        var declarationModel = blackboardVariableField.VariableDeclarationModel as VariableDeclarationModel;
                        if (declarationModel != null && declarationModel.FunctionModel != null)
                            FetchVariableDeclarationModelsFromFunctionModel(declarationModel.FunctionModel, ref declarationModelTuples);
                    }
                    break;
                }
            }
        }

        public HashSet<Tuple<IVariableDeclarationModel, bool>> GetAllVariableDeclarationsFromSelection(List<ISelectable> selection)
        {
            HashSet<Tuple<IVariableDeclarationModel, bool>> declarationModelTuples = new HashSet<Tuple<IVariableDeclarationModel, bool>>();
            FindVariableDeclarationsFromSelection(selection, new HashSet<INodeModel>(), declarationModelTuples);

            return declarationModelTuples;
        }

        // TODO:
        // 1) Move to Node extension method
        // 2) Use predicate which returns true as soon as we find graphElementModel that corresponds to node's graphElementModel
        public static bool IsNodeConnectedTo(Experimental.GraphView.Node node,
            Experimental.GraphView.Node otherNode)
        {
            if (node == null)
                return false;

            var otherGraphElementModel = (otherNode as IHasGraphElementModel)?.GraphElementModel;
            if (otherGraphElementModel == null)
                return false;

            // Fetch all nodes first
            HashSet<Experimental.GraphView.Node> visitedNodes =
                new HashSet<Experimental.GraphView.Node>();
            VisitNodes(node, ref visitedNodes);

            return visitedNodes.Any(x =>
                x is IHasGraphElementModel hasGraphElementModel &&
                ReferenceEquals(hasGraphElementModel.GraphElementModel, otherGraphElementModel));
        }
    }
}
