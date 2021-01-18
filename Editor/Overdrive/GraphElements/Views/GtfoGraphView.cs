using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
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
            public List<INodeModel> nodes;
            public List<IEdgeModel> edges;
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

        public event Action<List<ISelectableGraphElement>> OnSelectionChangedCallback;

        bool m_SelectionDraggerWasActive;
        Vector2 m_LastMousePosition;

        SelectionDragger m_SelectionDragger;
        ContentDragger m_ContentDragger;
        Clickable m_Clickable;
        RectangleSelector m_RectangleSelector;
        FreehandSelector m_FreehandSelector;

        protected SelectionDragger SelectionDragger
        {
            get => m_SelectionDragger;
            set => this.ReplaceManipulator(ref m_SelectionDragger, value);
        }

        protected ContentDragger ContentDragger
        {
            get => m_ContentDragger;
            set => this.ReplaceManipulator(ref m_ContentDragger, value);
        }

        protected Clickable Clickable
        {
            get => m_Clickable;
            set => this.ReplaceManipulator(ref m_Clickable, value);
        }

        protected RectangleSelector RectangleSelector
        {
            get => m_RectangleSelector;
            set => this.ReplaceManipulator(ref m_RectangleSelector, value);
        }

        public FreehandSelector FreehandSelector
        {
            get => m_FreehandSelector;
            set => this.ReplaceManipulator(ref m_FreehandSelector, value);
        }

        protected IExternalDragNDropHandler m_CurrentDragNDropHandler;

        // Cache for INodeModelProxies
        Dictionary<Type, INodeModelProxy> m_ModelProxies;

        public new GtfoWindow Window => base.Window as GtfoWindow;

        protected GtfoGraphView(GraphViewEditorWindow window, Store store, string uniqueGraphViewName)
            : base(window, store)
        {
            // This is needed for selection persistence.
            viewDataKey = uniqueGraphViewName;

            name = uniqueGraphViewName;

            m_ModelProxies = new Dictionary<Type, INodeModelProxy>();

            SetupZoom(minScaleSetup: .1f, maxScaleSetup: 4f, 1.0f);

            Clickable = new Clickable(OnDoubleClick);
            Clickable.activators.Clear();
            Clickable.activators.Add(
                new ManipulatorActivationFilter { button = MouseButton.LeftMouse, clickCount = 2 });

            ContentDragger = new ContentDragger();
            SelectionDragger = new SelectionDragger(this);
            RectangleSelector = new RectangleSelector();
            FreehandSelector = new FreehandSelector();

            RegisterCallback<MouseOverEvent>(OnMouseOver);
            RegisterCallback<MouseMoveEvent>(OnMouseMove);

            // TODO: Until GraphView.SelectionDragger is used widely in VS, we must register to drag events ON TOP of
            // using the VisualScripting.Editor.SelectionDropper, just to deal with drags from the Blackboard
            RegisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);
            RegisterCallback<DragExitedEvent>(OnDragExitedEvent);
            RegisterCallback<DragPerformEvent>(OnDragPerformEvent);
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);

            Insert(0, new GridBackground());

            SerializeGraphElementsCallback = OnSerializeGraphElements;
            UnserializeAndPasteCallback = UnserializeAndPaste;
        }

        internal void UnloadGraph()
        {
            Blackboard?.Clear();
            ClearGraph();
        }

        void ClearGraph()
        {
            List<GraphElement> elements = GraphElements.ToList();

            PositionDependenciesManager.Clear();
            foreach (var element in elements)
            {
                RemoveElement(element, false);
            }
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
            UnityEditor.Selection.activeObject = Store.State?.AssetModel as Object;
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

        void OnMouseMove(MouseMoveEvent evt)
        {
            if (m_SelectionDraggerWasActive && !m_SelectionDragger.IsActive) // cancelled
            {
                m_SelectionDraggerWasActive = false;
                PositionDependenciesManager.CancelMove();
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
                    PositionDependenciesManager.StartNotifyMove(Selection, startPos);

                // schedule execute because the mouse won't be moving when the graph view is panning
                schedule.Execute(() =>
                {
                    if (m_SelectionDragger.IsActive && moveNodeDependencies) // processed
                    {
                        Vector2 pos = contentViewContainer.ChangeCoordinatesTo(elem.hierarchy.parent, elem.GetPosition().position);
                        PositionDependenciesManager.ProcessMovedNodes(pos);
                    }
                }).Until(() => !m_SelectionDraggerWasActive);
            }

            m_LastMousePosition = this.ChangeCoordinatesTo(contentViewContainer, evt.localMousePosition);
        }

        public override void StopSelectionDragger()
        {
            // cancellation is handled in the MoveMove callback
            m_SelectionDraggerWasActive = false;
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

        public override void AddElement(GraphElement graphElement)
        {
            // exception thrown by graphview while in playmode
            // probably related to the undo selection thingy
            try
            {
                base.AddElement(graphElement);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            if (graphElement?.Model != null &&
                Store.State.SelectionStateComponent.ShouldSelectElementUponCreation(graphElement.Model))
            {
                graphElement.Select(this, true);
            }

            if (graphElement is Node || graphElement is Edge)
                graphElement.RegisterCallback<MouseOverEvent>(OnMouseOver);

            if (graphElement?.Model is IEdgePortalModel portalModel)
            {
                AddPortalDependency(portalModel);
            }
        }

        public override void RemoveElement(GraphElement graphElement, bool unselectBeforeRemove = false)
        {
            var graphElementModel = graphElement.Model;
            switch (graphElementModel)
            {
                case IEdgeModel e:
                    RemovePositionDependency(e);
                    break;
                case IEdgePortalModel portalModel:
                    RemovePortalDependency(portalModel);
                    break;
            }

            if (graphElement is Node || graphElement is Edge)
                graphElement.UnregisterCallback<MouseOverEvent>(OnMouseOver);

            base.RemoveElement(graphElement, unselectBeforeRemove);
        }

        public virtual void UpdateUI(UIRebuildType rebuildType)
        {
            if (rebuildType == UIRebuildType.Complete || Store.State.NewModels.Any())
            {
                RebuildAll(Store.State);

                // PF FIXME: This should not be necessary
                this.HighlightGraphElements();
            }
            else if (rebuildType == UIRebuildType.Partial)
            {
                foreach (var model in Store.State.DeletedModels)
                {
                    foreach (var ui in model.GetAllUIs(this).ToList())
                    {
                        RemoveElement(ui as GraphElement, true);
                    }

                    foreach (var ui in model.GetDependencies().ToList())
                    {
                        ui.UpdateFromModel();
                    }
                }

                foreach (var model in Store.State.ChangedModels)
                {
                    foreach (var ui in model.GetAllUIs(this).ToList())
                    {
                        ui.UpdateFromModel();
                    }

                    foreach (var ui in model.GetDependencies().ToList())
                    {
                        ui.UpdateFromModel();
                    }
                }

                // PF FIXME: This should not be necessary
                this.HighlightGraphElements();
            }

            // PF FIXME: node state (enable/disabled, used/unused) should be part of the State.
            if (Store.State.Preferences.GetBool(BoolPref.ShowUnusedNodes))
                PositionDependenciesManager.UpdateNodeState();

            if (Store.State.ModelsToAutoAlign.Any())
            {
                // Auto placement relies on UI layout to compute node positions, so we need to
                // schedule it to execute after the next layout pass.
                // Furthermore, it will modify the model position, hence it must be
                // done inside a Store.BeginStateChange block.
                var elementsToAlign = Store.State.ModelsToAutoAlign.ToList();
                schedule.Execute(() =>
                {
                    Store.BeginStateChange();
                    PositionDependenciesManager.AlignNodes(true, elementsToAlign);
                    Store.EndStateChange();
                });
            }
        }

        void RebuildAll(State state)
        {
            ClearGraph();

            var graphModel = state.GraphModel;
            if (graphModel == null)
                return;

            PlacematContainer.RemoveAllPlacemats();

            foreach (var nodeModel in graphModel.NodeModels)
            {
                var node = GraphElementFactory.CreateUI<GraphElement>(this, Store, nodeModel);
                if (node != null)
                    AddElement(node);
            }

            foreach (var stickyNoteModel in graphModel.StickyNoteModels)
            {
                var stickyNote = GraphElementFactory.CreateUI<GraphElement>(this, Store, stickyNoteModel);
                if (stickyNote != null)
                    AddElement(stickyNote);
            }

            int index = 0;
            foreach (var edge in graphModel.EdgeModels)
            {
                if (!RestoreEdge(edge))
                {
                    Debug.LogWarning($"Edge {index} cannot be restored: {edge}");
                }
                index++;
            }
            foreach (var placematModel in state.GraphModel.PlacematModels.OrderBy(e => e.ZOrder))
            {
                var placemat = GraphElementFactory.CreateUI<GraphElement>(this, Store, placematModel);
                if (placemat != null)
                    AddElement(placemat);
            }

            contentViewContainer.Add(BadgesParent);

            BadgesParent.Clear();
            foreach (var badgeModel in graphModel.BadgeModels)
            {
                if (badgeModel.ParentModel == null)
                    continue;

                var badge = GraphElementFactory.CreateUI<Badge>(this, Store, badgeModel);
                if (badge != null)
                {
                    AddElement(badge);
                }
            }

            // We need to do this after all graph elements are created.
            foreach (var placemat in PlacematContainer.Placemats)
            {
                placemat.UpdateFromModel();
            }

            Blackboard?.SetupBuildAndUpdate(state.BlackboardGraphModel, Store, this);
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
            var edge = GraphElementFactory.CreateUI<GraphElement>(this, Store, edgeModel);
            AddElement(edge);

            AddPositionDependency(edgeModel);
        }

        public void NotifyTopologyChange(IGraphModel graphModel)
        {
            viewDataKey = graphModel.AssetModel.GetPath();
        }

        protected virtual void OnDragUpdatedEvent(DragUpdatedEvent e)
        {
            if (DragAndDrop.objectReferences.Length > 0)
            {
                var stencil = Store.State.GraphModel.Stencil;
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
            var originalNodes = graphElementModels.OfType<INodeModel>().ToList();

            List<VariableDeclarationModel> variableDeclarationsToCopy = graphElementModels
                .OfType<VariableDeclarationModel>()
                .ToList();

            List<StickyNoteModel> stickyNotesToCopy = graphElementModels
                .OfType<StickyNoteModel>()
                .ToList();

            List<PlacematModel> placematsToCopy = graphElementModels
                .OfType<PlacematModel>()
                .ToList();

            List<IEdgeModel> edgesToCopy = graphElementModels
                .OfType<IEdgeModel>()
                .ToList();

            Vector2 topLeftNodePosition = Vector2.positiveInfinity;
            foreach (var n in originalNodes)
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
                nodes = originalNodes,
                edges = edgesToCopy,
                variableDeclarations = variableDeclarationsToCopy,
                stickyNotes = stickyNotesToCopy,
                placemats = placematsToCopy
            };

            return copyPasteData;
        }

        internal static void PasteSerializedData(IGraphModel graph, TargetInsertionInfo targetInfo, SelectionStateComponent selectionState, CopyPasteData copyPasteData)
        {
            var elementMapping = new Dictionary<string, IGraphElementModel>();

            if (copyPasteData.variableDeclarations.Any())
            {
                List<IVariableDeclarationModel> variableDeclarationModels =
                    copyPasteData.variableDeclarations.Cast<IVariableDeclarationModel>().ToList();
                List<IVariableDeclarationModel> duplicatedModels = new List<IVariableDeclarationModel>();

                foreach (var sourceModel in variableDeclarationModels)
                {
                    duplicatedModels.Add(graph.DuplicateGraphVariableDeclaration(sourceModel));
                }

                selectionState?.SelectElementsUponCreation(duplicatedModels, true);
            }

            var nodeMapping = new Dictionary<INodeModel, INodeModel>();
            foreach (var originalModel in copyPasteData.nodes)
            {
                if (!graph.Stencil.CanPasteNode(originalModel, graph))
                    continue;

                var pastedNode = PasteNode(targetInfo.OperationName, originalModel, graph, selectionState, targetInfo.Delta);
                nodeMapping[originalModel] = pastedNode;
            }

            foreach (var nodeModel in nodeMapping)
            {
                elementMapping.Add(nodeModel.Key.Guid.ToString(), nodeModel.Value);
            }

            foreach (var edge in copyPasteData.edges)
            {
                elementMapping.TryGetValue(edge.ToNodeGuid.ToString(), out var newInput);
                elementMapping.TryGetValue(edge.FromNodeGuid.ToString(), out var newOutput);

                var copiedEdge = graph.DuplicateEdge(edge, newInput as INodeModel, newOutput as INodeModel);
                if (copiedEdge != null)
                {
                    elementMapping.Add(edge.Guid.ToString(), copiedEdge);
                    selectionState?.SelectElementsUponCreation(new[] { copiedEdge }, true);
                }
            }

            foreach (var stickyNote in copyPasteData.stickyNotes)
            {
                var newPosition = new Rect(stickyNote.PositionAndSize.position + targetInfo.Delta, stickyNote.PositionAndSize.size);
                var pastedStickyNote = (StickyNoteModel)graph.CreateStickyNote(newPosition);
                pastedStickyNote.Title = stickyNote.Title;
                pastedStickyNote.Contents = stickyNote.Contents;
                pastedStickyNote.Theme = stickyNote.Theme;
                pastedStickyNote.TextSize = stickyNote.TextSize;
                selectionState?.SelectElementsUponCreation(new[] { pastedStickyNote }, true);
                elementMapping.Add(stickyNote.Guid.ToString(), pastedStickyNote);
            }

            List<PlacematModel> pastedPlacemats = new List<PlacematModel>();
            // Keep placemats relative order
            foreach (var placemat in copyPasteData.placemats.OrderBy(p => p.ZOrder))
            {
                var newPosition = new Rect(placemat.PositionAndSize.position + targetInfo.Delta, placemat.PositionAndSize.size);
                var newTitle = "Copy of " + placemat.Title;
                var pastedPlacemat = (PlacematModel)graph.CreatePlacemat(newPosition);
                pastedPlacemat.Title = newTitle;
                pastedPlacemat.Color = placemat.Color;
                pastedPlacemat.Collapsed = placemat.Collapsed;
                pastedPlacemat.HiddenElementsGuid = placemat.HiddenElementsGuid;
                selectionState?.SelectElementsUponCreation(new[] { pastedPlacemat }, true);
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

            CopyPasteData copyPasteData = s_LastCopiedData;// JsonUtility.FromJson<CopyPasteData>(data);
            var delta = m_LastMousePosition - copyPasteData.topLeftNodePosition;

            TargetInsertionInfo info;
            info.OperationName = operationName;
            info.Delta = delta;

            Store.Dispatch(new PasteSerializedDataAction(info, s_LastCopiedData));
        }

        static INodeModel PasteNode(string operationName, INodeModel copiedNode, IGraphModel graph,
            SelectionStateComponent selectionState, Vector2 delta)
        {
            var pastedNodeModel = graph.DuplicateNode(copiedNode, delta);
            selectionState?.SelectElementsUponCreation(new[] { pastedNodeModel }, true);
            return pastedNodeModel;
        }

        public void DisplayCompilationErrors()
        {
            ConsoleWindowBridge.RemoveLogEntries();

            var deletedBadges = Store.State.GraphModel.DeleteBadgesOfType<CompilerErrorBadgeModel>();
            var newBadges = new List<IGraphElementModel>();

            var lastCompilationResult = Store.State.CompilationStateComponent.GetLastResult();
            if (lastCompilationResult?.errors != null && lastCompilationResult.errors.Count > 0)
            {
                var graphAsset = Store.State.AssetModel;
                foreach (var error in lastCompilationResult.errors)
                {
                    if (error.sourceNode != null && !error.sourceNode.Destroyed)
                    {
                        var badgeModel = new CompilerErrorBadgeModel(error);
                        Store.State.GraphModel.AddBadge(badgeModel);
                        newBadges.Add(badgeModel);
                    }

                    if (graphAsset != null && graphAsset is Object asset)
                    {
                        var graphAssetPath = asset ? AssetDatabase.GetAssetPath(asset) : "<unknown>";
                        ConsoleWindowBridge.LogSticky(
                            $"{graphAssetPath}: {error.description}",
                            $"{graphAssetPath}@{error.sourceNodeGuid}",
                            error.isWarning ? LogType.Warning : LogType.Error,
                            LogOption.None,
                            asset.GetInstanceID());
                    }
                }
            }

            if (deletedBadges.Count > 0 || newBadges.Count > 0)
            {
                Store.BeginStateChange();
                Store.State.MarkDeleted(deletedBadges);
                Store.State.MarkNew(newBadges);
                Store.EndStateChange();
            }
        }

        public abstract bool CanAcceptDrop(List<ISelectableGraphElement> dragSelection);

        public abstract bool DragUpdated(DragUpdatedEvent evt, IEnumerable<ISelectableGraphElement> dragSelection, IDropTarget dropTarget, ISelection dragSource);

        public abstract bool DragPerform(DragPerformEvent evt, IEnumerable<ISelectableGraphElement> dragSelection, IDropTarget dropTarget, ISelection dragSource);

        public abstract bool DragEnter(DragEnterEvent evt, IEnumerable<ISelectableGraphElement> dragSelection, IDropTarget enteredTarget, ISelection dragSource);

        public abstract bool DragLeave(DragLeaveEvent evt, IEnumerable<ISelectableGraphElement> dragSelection, IDropTarget leftTarget, ISelection dragSource);

        public abstract bool DragExited();
    }
}
