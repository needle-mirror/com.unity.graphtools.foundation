using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    // Migration class for VseGraphView.
    // In the end, GtfoGraphView should be merged with GraphView.
    public abstract class GtfoGraphView : GraphView, IDropTarget
    {
        [Serializable]
        public class CopyPasteData
        {
            public List<NodeModel> nodes;
            public List<EdgeModel> edges;
            public List<VariableDeclarationModel> variableDeclarations;
            public Vector2 topLeftNodePosition;
            public List<StickyNoteModel> stickyNotes;
            public List<PlacematModel> placemats;

            public string ToJson()
            {
                return JsonUtility.ToJson(this);
            }

            public bool IsEmpty() => (!nodes.Any() && !edges.Any() &&
                !variableDeclarations.Any() && !stickyNotes.Any() && !placemats.Any());
        }

        public const float DragDropSpacer = 5f;

        static CopyPasteData s_LastCopiedData;

        public static GraphElement clickTarget;

        public event Action<List<ISelectableGraphElement>> OnSelectionChangedCallback;

        bool m_SelectionDraggerWasActive;
        Vector2 m_LastMousePosition;
        SelectionDragger m_SelectionDragger;
        VisualElement m_IconsParent;

        protected IExternalDragNDropHandler m_CurrentDragNDropHandler;

        // Cache for INodeModelProxies
        Dictionary<Type, INodeModelProxy> m_ModelProxies;

        IGraphModel m_LastGraphModel;
        public IGraphModel LastGraphModel => m_LastGraphModel;

        public IRenamableGraphElement ElementToRename { private get; set; }

        public new GtfoWindow Window => base.Window as GtfoWindow;

        Dictionary<IGraphElementModel, GraphElement> ModelsToNodeMapping { get; set; }

        protected GtfoGraphView(GraphViewEditorWindow window, Store store, string uniqueGraphViewName)
            : base(window, store)
        {
            // This is needed for selection persistence.
            viewDataKey = uniqueGraphViewName;

            name = uniqueGraphViewName;

            m_ModelProxies = new Dictionary<Type, INodeModelProxy>();

            SetupZoom(minScaleSetup: .1f, maxScaleSetup: 4f);

            var clickable = new Clickable(OnDoubleClick);
            clickable.activators.Clear();
            clickable.activators.Add(
                new ManipulatorActivationFilter { button = MouseButton.LeftMouse, clickCount = 2 });
            this.AddManipulator(clickable);

            this.AddManipulator(new ContentDragger());
            m_SelectionDragger = new SelectionDragger(this);
            this.AddManipulator(m_SelectionDragger);
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new FreehandSelector());

            RegisterCallback<MouseOverEvent>(OnMouseOver);

            // Order is important here: MouseUp event needs to be registered after adding the RectangleSelector
            // to let the selector adding elements to the selection's list before mouseUp event is fired.
            RegisterCallback<MouseUpEvent>(OnMouseUp);
            RegisterCallback<MouseMoveEvent>(OnMouseMove);

            // TODO: Until GraphView.SelectionDragger is used widely in VS, we must register to drag events ON TOP of
            // using the VisualScripting.Editor.SelectionDropper, just to deal with drags from the Blackboard
            RegisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);
            RegisterCallback<DragExitedEvent>(OnDragExitedEvent);
            RegisterCallback<DragPerformEvent>(OnDragPerformEvent);
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);

            Insert(0, new GridBackground());

            ElementResizedCallback = OnElementResized;

            SerializeGraphElementsCallback = OnSerializeGraphElements;
            UnserializeAndPasteCallback = UnserializeAndPaste;

            GraphViewChangedCallback += OnGraphViewChanged;

            m_IconsParent = new VisualElement { name = "iconsParent"};
            m_IconsParent.style.overflow = Overflow.Visible;
        }

        internal void UnloadGraph()
        {
            Blackboard?.ClearContents();
            Blackboard?.Clear();
            ClearGraph();
        }

        void ClearGraph()
        {
            List<GraphElement> elements = GraphElements.ToList();

            PositionDependenciesManagers.Clear();
            foreach (var element in elements)
            {
                RemoveElement(element);

                element.UnregisterCallback<MouseOverEvent>(OnMouseOver);
            }

            if (contentViewContainer.Contains(m_IconsParent))
                contentViewContainer.Remove(m_IconsParent);
        }

        void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            // Set the window min size from the graphView
            Window.AdjustWindowMinSize(new Vector2(resolvedStyle.minWidth.value, resolvedStyle.minHeight.value));
        }

        void OnDoubleClick()
        {
            // Display graph in inspector when clicking on background
            // TODO: displayed on double click ATM as this method overrides the Token.Select() which does not stop propagation
            UnityEditor.Selection.activeObject = Store.GetState()?.CurrentGraphModel.AssetModel as Object;
        }

        public void OnMouseOver(MouseOverEvent evt)
        {
            // Disregard the event if we're moving the mouse over the SmartSearch window.
            if (Children().Any(x =>
            {
                var fullName = x.GetType().FullName;
                return fullName?.Contains("SmartSearch") == true;
            }))
            {
                return;
            }

            evt.StopPropagation();
        }

        void OnMouseUp(MouseUpEvent evt)
        {
            var pickList = new List<VisualElement>();
            var pickElem = panel.PickAll(evt.mousePosition, pickList);
            GraphElement graphElement = pickElem?.GetFirstOfType<GraphElement>();

            clickTarget = graphElement;

            this.HighlightGraphElements();
        }

        void OnMouseMove(MouseMoveEvent evt)
        {
            if (m_SelectionDraggerWasActive && !m_SelectionDragger.IsActive) // cancelled
            {
                m_SelectionDraggerWasActive = false;
                PositionDependenciesManagers.CancelMove();
            }
            else if (!m_SelectionDraggerWasActive && m_SelectionDragger.IsActive) // started
            {
                m_SelectionDraggerWasActive = true;

                GraphElement elem = (GraphElement)Selection.FirstOrDefault(x => x is IGraphElement hasModel && hasModel.Model is INodeModel);
                if (elem == null)
                    return;

                INodeModel elemModel = (INodeModel)elem.Model;
                Vector2 elemPos = elemModel.Position;
                Vector2 startPos = contentViewContainer.ChangeCoordinatesTo(elem.hierarchy.parent, elemPos);

                bool requireShiftToMoveDependencies = !(elemModel.GraphModel?.Stencil?.MoveNodeDependenciesByDefault).GetValueOrDefault();
                bool hasShift = evt.modifiers.HasFlag(EventModifiers.Shift);
                bool moveNodeDependencies = requireShiftToMoveDependencies == hasShift;

                if (moveNodeDependencies)
                    PositionDependenciesManagers.StartNotifyMove(Selection, startPos);

                // schedule execute because the mouse won't be moving when the graph view is panning
                schedule.Execute(() =>
                {
                    if (m_SelectionDragger.IsActive && moveNodeDependencies) // processed
                    {
                        Vector2 pos = contentViewContainer.ChangeCoordinatesTo(elem.hierarchy.parent, elem.GetPosition().position);
                        PositionDependenciesManagers.ProcessMovedNodes(pos);
                    }
                }).Until(() => !m_SelectionDraggerWasActive);
            }

            m_LastMousePosition = this.ChangeCoordinatesTo(contentViewContainer, evt.localMousePosition);
        }

        void OnElementResized(VisualElement element)
        {
            if (!(element is GraphElement ce))
                return;

            // Check for authorized size (GraphView manipulator might have resized to
            var proposedWidth = ce.style.width.value.value;
            var proposedHeight = ce.style.height.value.value;

            var maxAllowedWidth = ce.resolvedStyle.maxWidth.value - ce.resolvedStyle.left;
            var maxAllowedHeight = ce.resolvedStyle.maxHeight.value - ce.resolvedStyle.top;

            var newWidth = Mathf.Min(maxAllowedWidth, proposedWidth);
            var newHeight = Mathf.Min(maxAllowedHeight, proposedHeight);

            // Resize only if values have changed
            if (Math.Abs(proposedWidth - newWidth) > Mathf.Epsilon ||
                Math.Abs(proposedHeight - newHeight) > Mathf.Epsilon)
                ce.SetPosition(new Rect(ce.layout.x, ce.layout.y, newWidth, newHeight));
        }

        GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
        {
            if (graphViewChange.movedElements?.Any() != true)
                return graphViewChange;

            // cancellation is handled in the MoveMove callback
            m_SelectionDraggerWasActive = false;
            PositionDependenciesManagers.StopNotifyMove();

            Vector2 delta = graphViewChange.moveDelta;
            if (delta == Vector2.zero)
                return graphViewChange;

            return graphViewChange;
        }

        public override void ClearSelection()
        {
            Window.ClearNodeInSidePanel();
            base.ClearSelection();
        }

        public override void AddToSelection(ISelectableGraphElement selectable)
        {
            base.AddToSelection(selectable);

            // m_PersistedSelectionRestoreEnabled check: when clicking on a GO with the same graph while a token/node/
            // ... is selected, GraphView's selection restoration used to set the Selection.activeObject back to the item
            // !m_PersistedSelectionRestoreEnabled implies we're restoring the selection right now and should not set it
            if (PersistedSelectionRestoreEnabled &&
                selectable is IGraphElement hasModel)
            {
                if (!m_ModelProxies.TryGetValue(hasModel.Model.GetType(), out var currentProxy))
                {
                    var genericType = typeof(NodeModelProxy<>).MakeGenericType(hasModel.Model.GetType());
                    var derivedTypes = TypeCache.GetTypesDerivedFrom(genericType);
                    if (derivedTypes.Any())
                    {
                        var type = derivedTypes.FirstOrDefault();
                        currentProxy = (INodeModelProxy)ScriptableObject.CreateInstance(type);
                        m_ModelProxies.Add(hasModel.Model.GetType(), currentProxy);
                    }
                }
                currentProxy?.SetModel(hasModel.Model);
                var scriptableObject = currentProxy?.ScriptableObject();
                if (scriptableObject)
                    UnityEditor.Selection.activeObject = scriptableObject;
            }

            Window.ShowNodeInSidePanel(selectable, true);

            // TODO: convince UIElements to add proper support for Z Order as this won't survive a refresh
            // alternative: reorder models or store our own z order in models
            if (selectable is VisualElement visualElement)
            {
                if (visualElement is Node)
                    visualElement.BringToFront();
            }

            OnSelectionChangedCallback?.Invoke(Selection);
        }

        public override void RemoveFromSelection(ISelectableGraphElement selectable)
        {
            base.RemoveFromSelection(selectable);
            Window.ShowNodeInSidePanel(selectable, false);
            OnSelectionChangedCallback?.Invoke(Selection);
        }

        internal override void OnViewDataReady()
        {
            base.OnViewDataReady();

            if (Selection.OfType<Node>().Any())
            {
                Window.ShowNodeInSidePanel(Selection.OfType<Node>().Last(), true);
            }
            else
            {
                Window.ShowNodeInSidePanel(null, false);
            }
        }

        public virtual void UpdateTopology()
        {
            Profiler.BeginSample("UpdateTopology");
            Stopwatch topologyStopwatch = new Stopwatch();
            topologyStopwatch.Start();

            var state = Store.GetState();
            var currentGraphModel = state.CurrentGraphModel;
            if (currentGraphModel == null)
            {
                return;
            }

            GraphChangeList graphChangeList = currentGraphModel.LastChanges;
            string dispatchedActionName = state.LastDispatchedActionName; // save this now, because some actions trigger a UIRefresh, hiding the original action (TODO)

            DisablePersistedSelectionRestore();

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
                PositionDependenciesManagers.UpdateNodeState();

            this.HighlightGraphElements();

            m_LastGraphModel = currentGraphModel;

            EnablePersistedSelectionRestore();

            if (ElementToRename != null)
            {
                ClearSelection();
                AddToSelection((GraphElement)ElementToRename);
                ElementToRename.Rename(forceRename: true);
                ElementToRename = null;
                state.EditorDataModel.ElementModelToRename = null;
            }

            // We need to do this after all graph elements are created.
            foreach (var p in PlacematContainer.Placemats)
            {
                p.UpdateFromModel();
            }

            MarkDirtyRepaint();

            topologyStopwatch.Stop();
            Profiler.EndSample();

            if (state.Preferences.GetBool(BoolPref.WarnOnUIFullRebuild) && state.LastActionUIRebuildType == State.UIRebuildType.Full)
            {
                Debug.LogWarning($"Rebuilding the whole UI ({dispatchedActionName})");
            }

            if (state.Preferences.GetBool(BoolPref.LogUIBuildTime))
            {
                Debug.Log($"UI Update ({dispatchedActionName}) took {topologyStopwatch.ElapsedMilliseconds} ms");
            }
        }

        GraphElement CreateElement(IGraphElementModel model)
        {
            GraphElement element = model.CreateUI<GraphElement>(this, Store);
            if (element != null)
                AddToGraphView(element);

            return element;
        }

        void AddToGraphView(GraphElement graphElement)
        {
            // exception thrown by graphview while in playmode
            // probably related to the undo selection thingy
            try
            {
                if (graphElement.parent == null) // Some elements (e.g. Placemats) come in already added to the right spot.
                    AddElement(graphElement);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            if (graphElement != null &&
                Store.GetState().EditorDataModel.ShouldSelectElementUponCreation(graphElement.Model))
                graphElement.Select(this, true);

            if (graphElement is Node || graphElement is Edge)
                graphElement.RegisterCallback<MouseOverEvent>(OnMouseOver);

            if (graphElement?.Model is IEdgePortalModel portalModel)
            {
                AddPortalDependency(portalModel);
            }
        }

        void RemoveFromGraphView(GraphElement graphElement)
        {
            switch (graphElement)
            {
                case Edge e:
                    RemovePositionDependency(e.EdgeModel);
                    break;
                case Node n when n.NodeModel is IEdgePortalModel portalModel:
                    RemovePortalDependency(portalModel);
                    break;
            }

            graphElement.Unselect(this);
            DeleteElements(new[] { graphElement });
            graphElement.UnregisterCallback<MouseOverEvent>(OnMouseOver);
        }

        void PartialRebuild(State state)
        {
            state.LastActionUIRebuildType = State.UIRebuildType.Partial;

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
                    Blackboard?.Rebuild(Blackboard.RebuildMode.BlackboardOnly);
                }

                if (state.Preferences.GetBool(BoolPref.LogUIBuildTime))
                {
                    Debug.Log(partialRebuilder.DebugOutput);
                }
            }
        }

        void RebuildAll(State state)
        {
            state.LastActionUIRebuildType = State.UIRebuildType.Full;
            ClearGraph();

            var graphModel = state.CurrentGraphModel;
            if (graphModel == null)
                return;

            PlacematContainer.RemoveAllPlacemats();
            foreach (var placematModel in state.CurrentGraphModel.PlacematModels.OrderBy(e => e.ZOrder))
            {
                var placemat = GraphElementFactory.CreateUI<GraphElement>(this, Store, placematModel);
                if (placemat != null)
                    AddToGraphView(placemat);
            }

            foreach (var nodeModel in graphModel.NodeModels)
            {
                var node = GraphElementFactory.CreateUI<GraphElement>(this, Store, nodeModel);
                if (node != null)
                    AddToGraphView(node);
            }

            foreach (var stickyNoteModel in state.CurrentGraphModel.StickyNoteModels)
            {
                var stickyNote = GraphElementFactory.CreateUI<GraphElement>(this, Store, stickyNoteModel);
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

            Blackboard?.Rebuild(Blackboard.RebuildMode.BlackboardOnly);

            contentViewContainer.Add(m_IconsParent);
            this.HighlightGraphElements();
        }

        void MapModelsToNodes()
        {
            var hasGraphElementModels = this.Query<GraphElement>()
                .Where(x => !(x is BlackboardVariableField) && x.Model != null)
                .ToList();

            ModelsToNodeMapping = hasGraphElementModels
                .GroupBy(x => x.Model, x => x)
                .ToDictionary(g => g.Key, g => g.First());
        }

        bool RestoreEdge(IEdgeModel edge)
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
                    var inputNodeUi = inputNode?.GetUI(this);
                    inputNodeUi?.UpdateFromModel();

                    var outputNodeUi = outputNode?.GetUI(this);
                    outputNodeUi?.UpdateFromModel();

                    Connect(edge);
                    return true;
                }
            }

            return false;
        }

        void Connect(IEdgeModel edgeModel)
        {
            var edge = GraphElementFactory.CreateUI<Edge>(this, Store, edgeModel);
            AddToGraphView(edge);

            AddPositionDependency(edge.EdgeModel);
        }

        internal void AttachValue(IBadgeContainer badgeContainer, VisualElement visualElement, string value, Color badgeColor, SpriteAlignment alignment)
        {
            Assert.IsNotNull(visualElement);

            badgeContainer.ShowValueBadge(m_IconsParent, visualElement, alignment, value, badgeColor);
        }

        internal void AttachErrorBadge(VisualElement visualElement, string errorDescription, SpriteAlignment alignment, Store store = null, CompilerQuickFix errorQuickFix = null)
        {
            Assert.IsNotNull(visualElement);
            if (errorQuickFix != null)
                Assert.IsNotNull(store);

            VisualElement visualElementParent = visualElement.parent;
            while (visualElementParent.GetType().IsSubclassOf(typeof(GraphElement)) && visualElementParent.parent != null)
            {
                visualElementParent = visualElementParent.parent;
            }

            (visualElement as IBadgeContainer)?.ShowErrorBadge(m_IconsParent, alignment, errorDescription, store, errorQuickFix);
        }

        public void NotifyTopologyChange(IGraphModel graphModel)
        {
            viewDataKey = graphModel.GetAssetPath();
        }

        protected virtual void OnDragUpdatedEvent(DragUpdatedEvent e)
        {
            if (DragAndDrop.objectReferences.Length > 0)
            {
                var stencil = Store.GetState().CurrentGraphModel.Stencil;
                m_CurrentDragNDropHandler = stencil.DragNDropHandler;
                m_CurrentDragNDropHandler?.HandleDragUpdated(e, DragNDropContext.Graph);
            }
            e.StopPropagation();
        }

        protected virtual void OnDragPerformEvent(DragPerformEvent e)
        {
            if (m_CurrentDragNDropHandler != null)
            {
                m_CurrentDragNDropHandler.HandleDragPerform(e, Store, DragNDropContext.Graph, contentViewContainer);
                m_CurrentDragNDropHandler = null;
            }
            e.StopPropagation();
        }

        protected virtual void OnDragExitedEvent(DragExitedEvent e)
        {
            m_CurrentDragNDropHandler = null;
        }

        public static string OnSerializeGraphElements(IEnumerable<GraphElement> elements)
        {
            var copyPasteData = GatherCopiedElementsData(elements
                .Select(x => x.Model)
                .ToList());
            s_LastCopiedData = copyPasteData;
            return copyPasteData.IsEmpty() ? string.Empty : "data";// copyPasteData.ToJson();
        }

        internal static CopyPasteData GatherCopiedElementsData(IReadOnlyCollection<IGraphElementModel> graphElementModels)
        {
            IEnumerable<NodeModel> originalNodes = graphElementModels.OfType<NodeModel>();
            List<NodeModel> nodesToCopy = originalNodes
                .ToList();

            IEnumerable<INodeModel> floatingNodes = graphElementModels.OfType<INodeModel>();

            List<VariableDeclarationModel> variableDeclarationsToCopy = graphElementModels
                .OfType<VariableDeclarationModel>()
                .ToList();

            List<StickyNoteModel> stickyNotesToCopy = graphElementModels
                .OfType<StickyNoteModel>()
                .ToList();

            List<PlacematModel> placematsToCopy = graphElementModels
                .OfType<PlacematModel>()
                .ToList();

            List<EdgeModel> edgesToCopy = graphElementModels
                .OfType<EdgeModel>()
                .ToList();

            Vector2 topLeftNodePosition = Vector2.positiveInfinity;
            foreach (var n in floatingNodes)
            {
                topLeftNodePosition = Vector2.Min(topLeftNodePosition, n.Position);
            }
            foreach (var n in stickyNotesToCopy)
            {
                topLeftNodePosition = Vector2.Min(topLeftNodePosition, n.PositionAndSize.position);
            }
            foreach (var n in placematsToCopy)
            {
                topLeftNodePosition = Vector2.Min(topLeftNodePosition, n.PositionAndSize.position);
            }
            if (topLeftNodePosition == Vector2.positiveInfinity)
            {
                topLeftNodePosition = Vector2.zero;
            }

            CopyPasteData copyPasteData = new CopyPasteData
            {
                topLeftNodePosition = topLeftNodePosition,
                nodes = nodesToCopy,
                edges = edgesToCopy,
                variableDeclarations = variableDeclarationsToCopy,
                stickyNotes = stickyNotesToCopy,
                placemats = placematsToCopy
            };

            return copyPasteData;
        }

        internal static void PasteSerializedData(IGraphModel graph, TargetInsertionInfo targetInfo, IEditorDataModel editorDataModel, CopyPasteData copyPasteData)
        {
            var elementMapping = new Dictionary<string, IGraphElementModel>();

            var nodeMapping = new Dictionary<INodeModel, INodeModel>();
            foreach (var originalModel in copyPasteData.nodes)
            {
                if (!graph.Stencil.CanPasteNode(originalModel, graph))
                    continue;

                PasteNode(targetInfo.OperationName, originalModel, nodeMapping, graph, editorDataModel, targetInfo.Delta);
            }

            foreach (var nodeModel in nodeMapping)
            {
                elementMapping.Add(nodeModel.Key.Guid.ToString(), nodeModel.Value);
            }

            foreach (var edge in copyPasteData.edges)
            {
                elementMapping.TryGetValue(edge.ToNodeGuid.ToString(), out var newInput);
                elementMapping.TryGetValue(edge.FromNodeGuid.ToString(), out var newOutput);

                IPortModel inputPortModel = null;
                IPortModel outputPortModel = null;
                if (newInput != null && newOutput != null)
                {
                    // Both node were duplicated; create a new edge between the duplicated nodes.
                    inputPortModel = (newInput as IInOutPortsNode)?.InputsById[edge.ToPortId];
                    outputPortModel = (newOutput as IInOutPortsNode)?.OutputsById[edge.FromPortId];
                }
                else if (newInput != null)
                {
                    inputPortModel = (newInput as IInOutPortsNode)?.InputsById[edge.ToPortId];
                    outputPortModel = edge.FromPort;
                }
                else if (newOutput != null)
                {
                    inputPortModel = edge.ToPort;
                    outputPortModel = (newOutput as IInOutPortsNode)?.OutputsById[edge.FromPortId];
                }

                if (inputPortModel != null && outputPortModel != null)
                {
                    if (inputPortModel.Capacity == PortCapacity.Single && inputPortModel.GetConnectedEdges().Any())
                        continue;
                    if (outputPortModel.Capacity == PortCapacity.Single && outputPortModel.GetConnectedEdges().Any())
                        continue;

                    var copiedEdge = graph.CreateEdge(inputPortModel, outputPortModel);
                    elementMapping.Add(edge.GetId(), copiedEdge);
                    editorDataModel?.SelectElementsUponCreation(new[] { copiedEdge }, true);
                }
            }

            if (copyPasteData.variableDeclarations.Any())
            {
                List<IVariableDeclarationModel> variableDeclarationModels =
                    copyPasteData.variableDeclarations.Cast<IVariableDeclarationModel>().ToList();

                var duplicatedModels = graph.DuplicateGraphVariableDeclarations(variableDeclarationModels);
                editorDataModel?.SelectElementsUponCreation(duplicatedModels, true);
            }

            foreach (var stickyNote in copyPasteData.stickyNotes)
            {
                var newPosition = new Rect(stickyNote.PositionAndSize.position + targetInfo.Delta, stickyNote.PositionAndSize.size);
                var pastedStickyNote = (StickyNoteModel)graph.CreateStickyNote(newPosition);
                pastedStickyNote.Title = stickyNote.Title;
                pastedStickyNote.Contents = stickyNote.Contents;
                pastedStickyNote.Theme = stickyNote.Theme;
                pastedStickyNote.TextSize = stickyNote.TextSize;
                editorDataModel?.SelectElementsUponCreation(new[] { pastedStickyNote }, true);
                elementMapping.Add(stickyNote.Guid.ToString(), pastedStickyNote);
            }

            List<PlacematModel> pastedPlacemats = new List<PlacematModel>();
            // Keep placemats relative order
            foreach (var placemat in copyPasteData.placemats.OrderBy(p => p.ZOrder))
            {
                var newPosition = new Rect(placemat.PositionAndSize.position + targetInfo.Delta, placemat.PositionAndSize.size);
                var newTitle = "Copy of " + placemat.Title;
                var pastedPlacemat = (PlacematModel)graph.CreatePlacemat(newTitle, newPosition);
                pastedPlacemat.Color = placemat.Color;
                pastedPlacemat.Collapsed = placemat.Collapsed;
                pastedPlacemat.HiddenElementsGuid = placemat.HiddenElementsGuid;
                editorDataModel?.SelectElementsUponCreation(new[] { pastedPlacemat }, true);
                pastedPlacemats.Add(pastedPlacemat);
                elementMapping.Add(placemat.Guid.ToString(), pastedPlacemat);
            }

            // Update hidden content to new node ids.
            foreach (var pastedPlacemat in pastedPlacemats)
            {
                if (pastedPlacemat.Collapsed)
                {
                    List<string> pastedHiddenContent = new List<string>();
                    foreach (var guid in pastedPlacemat.HiddenElementsGuid)
                    {
                        IGraphElementModel pastedElement;
                        if (elementMapping.TryGetValue(guid, out pastedElement))
                        {
                            pastedHiddenContent.Add(pastedElement.Guid.ToString());
                        }
                    }

                    pastedPlacemat.HiddenElementsGuid = pastedHiddenContent;
                }
            }
        }

        void UnserializeAndPaste(string operationName, string data)
        {
            if (s_LastCopiedData == null || s_LastCopiedData.IsEmpty())//string.IsNullOrEmpty(data))
                return;

            ClearSelection();

            var graph = Store.GetState().CurrentGraphModel;
            CopyPasteData copyPasteData = s_LastCopiedData;// JsonUtility.FromJson<CopyPasteData>(data);
            var delta = m_LastMousePosition - copyPasteData.topLeftNodePosition;

            TargetInsertionInfo info;
            info.OperationName = operationName;
            info.Delta = delta;

            IEditorDataModel editorDataModel = Store.GetState().EditorDataModel;
            Store.Dispatch(new PasteSerializedDataAction(graph, info, editorDataModel, s_LastCopiedData));
        }

        static void PasteNode(string operationName, INodeModel copiedNode, Dictionary<INodeModel, INodeModel> mapping,
            IGraphModel graph, IEditorDataModel editorDataModel, Vector2 delta)
        {
            var pastedNodeModel = graph.DuplicateNode(copiedNode, mapping, delta);
            editorDataModel?.SelectElementsUponCreation(new[] { pastedNodeModel }, true);
        }

        public void DisplayCompilationErrors(State state)
        {
            ConsoleWindowBridge.RemoveLogEntries();
            if (ModelsToNodeMapping == null)
                UpdateTopology();

            var lastCompilationResult = state.CompilationResultModel.GetLastResult();
            if (lastCompilationResult?.errors == null)
                return;

            var graphAsset = (GraphAssetModel)Store.GetState().CurrentGraphModel?.AssetModel;
            foreach (var error in lastCompilationResult.errors)
            {
                if (error.sourceNode != null && !error.sourceNode.Destroyed)
                {
                    var alignment = SpriteAlignment.RightCenter;

                    var graphElement = error.sourceNode.GetUI(this);
                    if (graphElement != null)
                        AttachErrorBadge(graphElement, error.description, alignment, Store, error.quickFix);
                }

                if (graphAsset != null && graphAsset)
                {
                    var graphAssetPath = graphAsset ? AssetDatabase.GetAssetPath(graphAsset) : "<unknown>";
                    ConsoleWindowBridge.LogSticky(
                        $"{graphAssetPath}: {error.description}",
                        $"{graphAssetPath}@{error.sourceNodeGuid}",
                        error.isWarning ? LogType.Warning : LogType.Error,
                        LogOption.None,
                        graphAsset.GetInstanceID());
                }
            }
        }

        public void ClearCompilationErrors()
        {
            this.Query().Descendents<IconBadge>().ForEach(badge =>
            {
                badge.Detach();
                badge.RemoveFromHierarchy();
            });
        }

        public abstract bool CanAcceptDrop(List<ISelectableGraphElement> dragSelection);

        public abstract bool DragUpdated(DragUpdatedEvent evt, IEnumerable<ISelectableGraphElement> dragSelection, IDropTarget dropTarget, ISelection dragSource);

        public abstract bool DragPerform(DragPerformEvent evt, IEnumerable<ISelectableGraphElement> dragSelection, IDropTarget dropTarget, ISelection dragSource);

        public abstract bool DragEnter(DragEnterEvent evt, IEnumerable<ISelectableGraphElement> dragSelection, IDropTarget enteredTarget, ISelection dragSource);

        public abstract bool DragLeave(DragLeaveEvent evt, IEnumerable<ISelectableGraphElement> dragSelection, IDropTarget leftTarget, ISelection dragSource);

        public abstract bool DragExited();
    }
}
