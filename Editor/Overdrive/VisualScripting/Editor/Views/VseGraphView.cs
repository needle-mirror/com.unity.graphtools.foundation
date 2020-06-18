using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.Highlighting;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public class VseGraphView : GraphView, IDropTarget
    {
        public const double SlowDoubleClickSecs = 0.25f;
        public const double SlowDoubleClickMaxTimeElapsed = 0.75f;

        internal const float DragDropSpacer = 5f;

        readonly VseWindow m_Window;
        readonly Store m_Store;

        public VisualElement lastHoveredVisualElement;
        public VisualElement lastHoveredSmartSearchCompatibleElement;

        Vector2 m_LastMousePosition;
        bool m_DragStarted;

        List<TokenDeclaration> m_DraggedPlaceholderTokens; // holds placeholder tokens that need to be deleted once drag is successful or canceled

        public Store store => m_Store;
        public VseWindow window => m_Window;

        bool m_PersistedSelectionRestoreEnabled;

        DebugDisplayElement ContentDebugDisplay { get; }

        public bool ShowDebug { get; set; }

        public VseUIController UIController { get; }

        public static GraphElement clickTarget;
        public static double clickTimeSinceStartupSecs;

        delegate void RestorePersistedSelectionDelegate(GraphView graphView, GraphElement element);
        delegate bool PersistentSelectionContainsDelegate(GraphView graphView, GraphElement element);

        static RestorePersistedSelectionDelegate RestorePersistedSelection { get; set; }
        static PersistentSelectionContainsDelegate PersistentSelectionContains { get; set; }

        bool m_SelectionDraggerWasActive;

        // Cache for INodeModelProxies
        Dictionary<Type, INodeModelProxy> m_ModelProxies;

        class VSSelectionDragger : SelectionDragger
        {
            public VSSelectionDragger(GraphView graphView)
                : base(graphView) {}

            public bool IsActive => m_Active;
        }

        public event Action<List<ISelectableGraphElement>> OnSelectionChangedCallback;

        public override bool supportsWindowedBlackboard => true;

        public VseGraphView(VseWindow window, Store store, string graphViewName = "VisualScript") : base(store)
        {
            viewDataKey = "VSEditor";

            AddToClassList("vseGraphView");

            name = graphViewName;

            m_Store = store;
            m_Window = window;
            m_PersistedSelectionRestoreEnabled = true;
            UIController = new VseUIController(this, m_Store);

            m_DraggedPlaceholderTokens = new List<TokenDeclaration>();
            m_ModelProxies = new Dictionary<Type, INodeModelProxy>();

            SetupZoom(minScaleSetup: .1f, maxScaleSetup: 4f);

            var clickable = new Clickable(OnDoubleClick);
            clickable.activators.Clear();
            clickable.activators.Add(
                new ManipulatorActivationFilter { button = MouseButton.LeftMouse, clickCount = 2 });
            this.AddManipulator(clickable);

            this.AddManipulator(new ContentDragger());
            var selectionDragger = new VSSelectionDragger(this);
            this.AddManipulator(selectionDragger);
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new FreehandSelector());

            RegisterCallback<MouseOverEvent>(OnMouseOver);

            // Order is important here: MouseUp event needs to be registered after adding the RectangleSelector
            // to let the selector adding elements to the selection's list before mouseUp event is fired.
            RegisterCallback<MouseUpEvent>(OnMouseUp);
            RegisterCallback<MouseMoveEvent>(evt =>
            {
                if (m_SelectionDraggerWasActive && !selectionDragger.IsActive) // cancelled
                {
                    m_SelectionDraggerWasActive = false;
                    PositionDependenciesManagers.CancelMove();
                }
                else if (!m_SelectionDraggerWasActive && selectionDragger.IsActive) // started
                {
                    m_SelectionDraggerWasActive = true;

                    GraphElement elem = (GraphElement)selection.FirstOrDefault(x => x is IHasGraphElementModel hasModel && hasModel.GraphElementModel is INodeModel);
                    if (elem == null)
                        return;

                    INodeModel elemModel = (INodeModel)((IHasGraphElementModel)elem).GraphElementModel;
                    Vector2 elemPos = elemModel.Position;
                    Vector2 startPos = contentViewContainer.ChangeCoordinatesTo(elem.hierarchy.parent, elemPos);

                    bool requireShiftToMoveDependencies = !(elemModel.VSGraphModel?.Stencil?.MoveNodeDependenciesByDefault).GetValueOrDefault();
                    bool hasShift = evt.modifiers.HasFlag(EventModifiers.Shift);
                    bool moveNodeDependencies = requireShiftToMoveDependencies == hasShift;

                    if (moveNodeDependencies)
                        PositionDependenciesManagers.StartNotifyMove(selection, startPos);

                    // schedule execute because the mouse won't be moving when the graph view is panning
                    schedule.Execute(() =>
                    {
                        if (selectionDragger.IsActive && moveNodeDependencies) // processed
                        {
                            Vector2 pos = contentViewContainer.ChangeCoordinatesTo(elem.hierarchy.parent, elem.GetPosition().position);
                            PositionDependenciesManagers.ProcessMovedNodes(pos);
                        }
                    }).Until(() => !m_SelectionDraggerWasActive);
                }

                m_LastMousePosition = this.ChangeCoordinatesTo(contentViewContainer, evt.localMousePosition);
            });

            // TODO: Until GraphView.SelectionDragger is used widely in VS, we must register to drag events ON TOP of
            // using the VisualScripting.Editor.SelectionDropper, just to deal with drags from the Blackboard
            RegisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);
            RegisterCallback<DragExitedEvent>(OnDragExitedEvent);
            RegisterCallback<DragPerformEvent>(OnDragPerformEvent);
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);

            Insert(0, new GridBackground());

            elementResized = OnElementResized;

            serializeGraphElements = OnSerializeGraphElements;
            unserializeAndPaste = UnserializeAndPaste;

            graphViewChanged += OnGraphViewChanged;
            PositionDependenciesManagers = new PositionDependenciesManager(this, store.GetState().Preferences);

            // TODO: Remove this if/when possible...this is necessary for the moment as
            // GraphView.RestorePersistedSelection is defined as internal, i.e. inaccessible from here
            MethodInfo m = typeof(GraphView).GetMethod("RestorePersitentSelectionForElement", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(m);
            RestorePersistedSelection = (RestorePersistedSelectionDelegate)m.CreateDelegate(typeof(RestorePersistedSelectionDelegate));

            m = typeof(GraphView).GetMethod("PersistentSelectionContains", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            PersistentSelectionContains = (PersistentSelectionContainsDelegate)m?.CreateDelegate(typeof(PersistentSelectionContainsDelegate));

            // Initialize Content debug display
            if (ContentDebugDisplay == null && DebugDisplayElement.Allowed)
            {
                ContentDebugDisplay = new DebugDisplayElement(this);
                contentViewContainer.Add(ContentDebugDisplay);
                ContentDebugDisplay.StretchToParentSize();
                ShowDebug = true; // since we enable it manually with Defines, might as well show it by default
            }
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

            List<IMovableGraphElement> movableElements = graphViewChange.movedElements.OfType<IMovableGraphElement>().ToList();
            foreach (var movedElement in movableElements)
            {
                movedElement.UpdatePinning();
            }

            return graphViewChange;
        }

        void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            // Set the window min size from the graphView
            m_Window.AdjustWindowMinSize(new Vector2(resolvedStyle.minWidth.value, resolvedStyle.minHeight.value));
        }

        void OnMouseUp(MouseUpEvent evt)
        {
            double editorTimeSinceStartup = EditorApplication.timeSinceStartup;

            var pickList = new List<VisualElement>();
            var pickElem = panel.PickAll(evt.mousePosition, pickList);
            GraphElement graphElement = pickElem?.GetFirstOfType<GraphElement>();

            if (graphElement != clickTarget)
                clickTimeSinceStartupSecs = editorTimeSinceStartup;
            clickTarget = graphElement;

            this.HighlightGraphElements();
        }

        // Expects pickPoint to be in world coordinates
        static IDropTarget GetDropTarget(Vector2 pickPoint, VisualElement target)
        {
            var pickList = new List<VisualElement>();
            var pickElem = target.panel.PickAll(pickPoint, pickList);
            bool targetIsGraphView = target is GraphView;

            IDropTarget dropTarget = null;
            foreach (var pickItem in pickList)
            {
                if (!targetIsGraphView)
                    continue;

                dropTarget = pickItem as IDropTarget;
                if (dropTarget == null)
                    continue;

                // found one
                break;
            }

            return dropTarget ?? pickElem as IDropTarget;
        }

        void OnDragUpdatedEvent(DragUpdatedEvent e)
        {
            if (DragAndDrop.objectReferences.Length > 0)
            {
                var stencil = m_Store.GetState().CurrentGraphModel.Stencil;
                m_CurrentDragNDropHandler = stencil.DragNDropHandler;
                m_CurrentDragNDropHandler?.HandleDragUpdated(e, DragNDropContext.Graph);
            }
            else
            {
                m_CurrentDragNDropHandler = null;

                bool containsBlackboardFields = selection.OfType<IVisualScriptingField>().Any();
                if (!containsBlackboardFields)
                    return;

                IDropTarget dropTarget = GetDropTarget(e.mousePosition, e.target as VisualElement);
                dropTarget?.DragUpdated(e, selection, dropTarget, UIController.Blackboard);

                if (dropTarget != null && dropTarget.CanAcceptDrop(selection))
                    DragAndDrop.visualMode = e.ctrlKey ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Move;
                else
                    DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
            }
            e.StopPropagation();
        }

        void OnDragPerformEvent(DragPerformEvent e)
        {
            if (m_CurrentDragNDropHandler != null)
            {
                m_CurrentDragNDropHandler.HandleDragPerform(e, m_Store, DragNDropContext.Graph, contentViewContainer);
                m_CurrentDragNDropHandler = null;
            }
            else
            {
                bool containsBlackboardFields = selection.OfType<IVisualScriptingField>().Any();
                if (!containsBlackboardFields)
                    return;

                IDropTarget dropTarget = GetDropTarget(e.mousePosition, e.target as VisualElement);
                dropTarget?.DragPerform(e, selection, dropTarget, UIController.Blackboard);
            }
            e.StopPropagation();
        }

        void OnDragExitedEvent(DragExitedEvent e)
        {
            m_CurrentDragNDropHandler = null;

            if (!selection.OfType<IVisualScriptingField>().Any())
                return;

            // TODO: How to differentiate between case where mouse has left a drop target window and a true drag operation abort?
            IDropTarget dropTarget = GetDropTarget(e.mousePosition, e.target as VisualElement);
            dropTarget?.DragExited();
            e.StopPropagation();
        }

        void OnDoubleClick()
        {
            // Display graph in inspector when clicking on background
            // TODO: displayed on double click ATM as this method overrides the Token.Select() which does not stop propagation
            Selection.activeObject = m_Store.GetState()?.CurrentGraphModel as Object;
        }

        [Serializable]
        [MovedFrom(false, "UnityEditor.VisualScripting.Editor", "Unity.GraphTools.Foundation.Overdrive.Editor")]
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
        static CopyPasteData s_LastCopiedData;
        public static string OnSerializeGraphElements(IEnumerable<GraphElement> elements)
        {
            var copyPasteData = GatherCopiedElementsData(elements
                .OfType<IHasGraphElementModel>().Select(x => x.GraphElementModel)
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

        internal static void OnUnserializeAndPaste(VSGraphModel graph, TargetInsertionInfo targetInfo, IEditorDataModel editorDataModel, CopyPasteData data)
        {
            CopyPasteData copyPasteData = data;

            CreateElementsFromCopiedData(graph, targetInfo, editorDataModel, copyPasteData, out _);
        }

        static void CreateElementsFromCopiedData(VSGraphModel graph, TargetInsertionInfo targetInfo,
            IEditorDataModel editorDataModel,
            CopyPasteData copyPasteData,
            out Dictionary<INodeModel, NodeModel> nodeMapping)
        {
            Dictionary<string, IGraphElementModel> elementMapping = new Dictionary<string, IGraphElementModel>();

            nodeMapping = new Dictionary<INodeModel, NodeModel>();
            foreach (NodeModel originalModel in copyPasteData.nodes)
            {
                if (!graph.Stencil.CanPasteNode(originalModel, graph))
                    continue;

                PasteNode(targetInfo.OperationName, originalModel, nodeMapping, graph, editorDataModel, targetInfo.Delta);
            }

            foreach (var nodeModel in nodeMapping)
            {
                elementMapping.Add(nodeModel.Key.GetId(), nodeModel.Value);
            }

            foreach (var edge in copyPasteData.edges)
            {
                elementMapping.TryGetValue(edge.InputNodeGuid.ToString(), out var newInput);
                elementMapping.TryGetValue(edge.OutputNodeGuid.ToString(), out var newOutput);

                IPortModel inputPortModel = null;
                IPortModel outputPortModel = null;
                if (newInput != null && newOutput != null)
                {
                    // Both node were duplicated; create a new edge between the duplicated nodes.
                    inputPortModel = (newInput as INodeModel)?.InputsById[edge.InputId];
                    outputPortModel = (newOutput as INodeModel)?.OutputsById[edge.OutputId];
                }
                else if (newInput != null)
                {
                    inputPortModel = (newInput as INodeModel)?.InputsById[edge.InputId];
                    outputPortModel = edge.OutputPortModel;
                }
                else if (newOutput != null)
                {
                    inputPortModel = edge.InputPortModel;
                    outputPortModel = (newOutput as INodeModel)?.OutputsById[edge.OutputId];
                }

                if (inputPortModel != null && outputPortModel != null)
                {
                    var gtfInput = inputPortModel as IGTFPortModel;
                    var gtfOutput = outputPortModel as IGTFPortModel;
                    if (gtfInput?.Capacity == PortCapacity.Single && gtfInput.ConnectedEdges.Any())
                        continue;
                    if (gtfOutput?.Capacity == PortCapacity.Single && gtfOutput.ConnectedEdges.Any())
                        continue;

                    IEdgeModel copiedEdge = graph.CreateEdge(inputPortModel, outputPortModel);
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
                elementMapping.Add(stickyNote.GetId(), pastedStickyNote);
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
                elementMapping.Add(placemat.GetId(), pastedPlacemat);
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
                            pastedHiddenContent.Add(pastedElement.GetId());
                        }
                    }

                    pastedPlacemat.HiddenElementsGuid = pastedHiddenContent;
                }
            }

            Undo.RegisterCompleteObjectUndo((Object)graph.AssetModel, $"{targetInfo.OperationName} selection");
        }

        void UnserializeAndPaste(string operationName, string data)
        {
            if (s_LastCopiedData == null || s_LastCopiedData.IsEmpty())//string.IsNullOrEmpty(data))
                return;

            ClearSelection();

            var graph = (VSGraphModel)m_Store.GetState().CurrentGraphModel;
            CopyPasteData copyPasteData = s_LastCopiedData;// JsonUtility.FromJson<CopyPasteData>(data);
            var delta = m_LastMousePosition - copyPasteData.topLeftNodePosition;

            VisualElement element = lastHoveredSmartSearchCompatibleElement;

            TargetInsertionInfo info;
            info.OperationName = operationName;
            info.Delta = delta;

            IEditorDataModel editorDataModel = m_Store.GetState().EditorDataModel;
            m_Store.Dispatch(new PasteSerializedDataAction(graph, info, editorDataModel, s_LastCopiedData));
        }

        static void PasteNode(string operationName, INodeModel copiedNode, Dictionary<INodeModel, NodeModel> mapping,
            VSGraphModel graph, IEditorDataModel editorDataModel, Vector2 delta)
        {
            Undo.RegisterCompleteObjectUndo((Object)graph.AssetModel, operationName);
            var pastedNodeModel = graph.DuplicateNode(copiedNode, mapping, delta);
            editorDataModel?.SelectElementsUponCreation(new[] { pastedNodeModel }, true);
        }

        public bool CanAcceptDrop(List<ISelectableGraphElement> dragSelection) =>
            !dragSelection.Any(x =>
                x is IVisualScriptingField visualScriptingField && !visualScriptingField.CanInstantiateInGraph());

        public bool DragUpdated(DragUpdatedEvent evt, IEnumerable<ISelectableGraphElement> dragSelection, IDropTarget dropTarget, ISelection dragSource)
        {
            DragSetup(evt, dragSelection, dropTarget, dragSource);
            return true;
        }

        public bool DragPerform(DragPerformEvent evt, IEnumerable<ISelectableGraphElement> dragSelection, IDropTarget dropTarget, ISelection dragSource)
        {
            var dragSelectionList = dragSelection.ToList();

            DragSetup(evt, dragSelectionList, dropTarget, dragSource);
            List<GraphElement> dropElements = dragSelectionList.OfType<GraphElement>().ToList();

            List<Tuple<IVariableDeclarationModel, Vector2>> variablesToCreate = DragAndDropHelper.ExtractVariablesFromDroppedElements(dropElements, this, evt.mousePosition);

            List<Node> droppedNodes = dropElements.OfType<Node>().ToList();

            if (droppedNodes.Any(e => !(e.NodeModel is IVariableModel)) && variablesToCreate.Any())
            {
                // fail because in the current setup this would mean dispatching several actions
                throw new ArgumentException("Unhandled case, dropping blackboard/variables fields and nodes at the same time");
            }

            if (variablesToCreate.Any())
                (m_Store.GetState().CurrentGraphModel?.Stencil)?.OnDragAndDropVariableDeclarations(m_Store, variablesToCreate);

            RemoveFromClassList("dropping");
            m_DragStarted = false;

            ClearPlaceholdersAfterDrag();

            return true;
        }

        public bool DragEnter(DragEnterEvent evt, IEnumerable<ISelectableGraphElement> dragSelection, IDropTarget enteredTarget, ISelection dragSource)
        {
            return true;
        }

        public bool DragLeave(DragLeaveEvent evt, IEnumerable<ISelectableGraphElement> dragSelection, IDropTarget leftTarget, ISelection dragSource)
        {
            return true;
        }

        public bool DragExited()
        {
            ClearPlaceholdersAfterDrag();
            RemoveFromClassList("dropping");
            return true;
        }

        // TODO: Seems like something that should be done by TokenDeclaration.OnDragStarting
        static void DetachTokenDeclaration(TokenDeclaration tokenDeclaration)
        {
            GraphView gView = tokenDeclaration.GetFirstOfType<GraphView>();
            var tokenParent = tokenDeclaration.parent;
            var childIndex = tokenDeclaration.FindIndexInParent();

            var placeHolderTokenDeclaration = tokenDeclaration.Clone();
            placeHolderTokenDeclaration.AddToClassList("placeHolder");
            tokenParent.Insert(childIndex, placeHolderTokenDeclaration);
            placeHolderTokenDeclaration.MarkDirtyRepaint();

            gView.Add(tokenDeclaration);
        }

        // TODO: Seems like something that should be moved to BlackboardField.OnDragStarting
        // TODO: Revive this code when we can truly drag objects on screen using GraphView
        // SelectionDragger or SelectionDropper
//        GraphElement DetachConvertedBlackboardField(IVisualScriptingField field)
//        {
//            GraphView gView = blackboardField.GetFirstAncestorOfType<GraphView>();
//            var newTokenDeclaration = new TokenDeclaration(m_Store, blackboardField.graphElementModel as IVariableDeclarationModel) {instantAdd = true};
//            gView.Add(newTokenDeclaration);
//            return newTokenDeclaration;
//            return null;
//        }

        void DragSetup(IMouseEvent mouseEvent, IEnumerable<ISelectableGraphElement> dragSelection, IDropTarget dropTarget, ISelection dragSource)
        {
            if (m_DragStarted)
                return;

            AddToClassList("dropping");

            if (dragSource != dropTarget || !(dragSource is IDropTarget))
            {
                // Drop into target graph view
                if (dropTarget is VseGraphView dropGraphView)
                {
                    var mousePosition = dropGraphView.contentViewContainer.WorldToLocal(mouseEvent.mousePosition);

                    var elementOffset = Vector2.zero;
                    foreach (var dropElement in dragSelection.OfType<VisualElement>())
                    {
                        var actualDropElement = dropElement as GraphElement;
                        var tokenDeclaration = dropElement as TokenDeclaration;

                        if (dragSource is Blackboard)
                        {
                            if (tokenDeclaration != null)
                                DetachTokenDeclaration(tokenDeclaration);
                            //Code is commented out until we uncomment DetachConvertedBlackboardField
                            if (dropElement is IVisualScriptingField) //blackboardField)
                                actualDropElement = null; //DetachConvertedBlackboardField(blackboardField);
                        }
                        else
                        {
                            Assert.IsTrue(dropElement is GraphElement);
                            dropGraphView.AddElement((GraphElement)dropElement);
                        }

                        if (actualDropElement == null)
                            continue;

                        actualDropElement.style.position = Position.Absolute;

                        if (tokenDeclaration != null)
                        {
                            mousePosition += elementOffset;
                            elementOffset.x += tokenDeclaration.ChangeCoordinatesTo(dropGraphView.contentViewContainer,
                                new Vector2(tokenDeclaration.layout.width + DragDropSpacer, 0f)).x;
                        }

                        var newPos = new Rect(mousePosition.x, mousePosition.y, dropElement.layout.width, dropElement.layout.height);
                        actualDropElement.SetPosition(newPos);

                        actualDropElement.MarkDirtyRepaint();
                    }
                }
            }

            m_DragStarted = true;
        }

        public override List<GraphElements.Port> GetCompatiblePorts(IGTFPortModel startPortModel, NodeAdapter nodeAdapter)
        {
            var startPort = startPortModel.GetUI<Port>(this);
            var startPortToken = startPort.GetFirstAncestorOfType<Token>();
            var startEdgePortalModel = startPortToken?.GraphElementModel as IEdgePortalModel;

            return ports.ToList().Where(p =>
            {
                var portToken = p.GetFirstAncestorOfType<Token>();

                var tokenInvolved = (startPortToken != null || portToken != null);
                var floatingNodeInvolved =
                    (startPort.node != null && startPort.node.ClassListContains("standalone")) ||
                    (p.node != null && p.node.ClassListContains("standalone"));

                if (startPort.PortModel.PortDataType == typeof(ExecutionFlow) && p.PortModel.PortDataType != typeof(ExecutionFlow))
                    return false;
                if (p.PortModel.PortDataType == typeof(ExecutionFlow) && startPort.PortModel.PortDataType != typeof(ExecutionFlow))
                    return false;

                // No good if ports belong to same node
                if (p == startPort ||
                    ((p.node != null || startPort.node != null) && (p.node == startPort.node)))
                    return false;

                // No good if it's on the same portal either.
                if (p.node is Token token && token.GraphElementModel is IEdgePortalModel edgePortalModel)
                {
                    if (edgePortalModel.DeclarationModel.GetId() == startEdgePortalModel?.DeclarationModel.GetId())
                        return false;
                }

                // This is true for all ports
                if (p.PortModel.Direction == startPort.PortModel.Direction)
                    return false;

                var currentPortModel = (PortModel)p.PortModel;
                if (tokenInvolved || floatingNodeInvolved)
                    return p.PortModel.Orientation == startPort.PortModel.Orientation && currentPortModel.PortType == (startPortModel as IPortModel)?.PortType;

                // Last resort: same orientation required
                return p.PortModel.Orientation == startPort.PortModel.Orientation;
            })

                // deep in GraphView's EdgeDragHelper, this list is used to find the first port to use when dragging an
                // edge. as ports are returned in hierarchy order (back to front), in case of a conflict, the one behind
                // the others is returned. reverse the list to get the most logical one, the one on top of everything else
                .Reverse()
                .ToList();
        }

        public EventPropagation RemoveSelection()
        {
            INodeModel[] selectedNodes = selection.OfType<IHasGraphElementModel>()
                .Select(x => x.GraphElementModel).OfType<INodeModel>().ToArray();

            INodeModel[] connectedNodes = selectedNodes.Where(x => x.InputsById.Values
                .Any(y => y.IsConnected) && x.OutputsById.Values.Any(y => y.IsConnected))
                .ToArray();

            bool canSelectionBeBypassed = connectedNodes.Any();
            if (canSelectionBeBypassed)
                m_Store.Dispatch(new RemoveNodesAction(connectedNodes, selectedNodes));
            else
                m_Store.Dispatch(new DeleteElementsAction(selectedNodes.Cast<IGTFGraphElementModel>().ToArray()));

            return selectedNodes.Any() ? EventPropagation.Stop : EventPropagation.Continue;
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

            lastHoveredVisualElement = (VisualElement)evt.target;

            while (lastHoveredVisualElement != null &&
                   !(lastHoveredVisualElement is Node ||
                     lastHoveredVisualElement is FakeNode ||
                     lastHoveredVisualElement is Token ||
                     lastHoveredVisualElement is VseGraphView ||
                     lastHoveredVisualElement.GetFirstOfType<TokenDeclaration>() != null))
            {
                lastHoveredVisualElement = lastHoveredVisualElement.parent;
            }

            lastHoveredSmartSearchCompatibleElement = (VisualElement)evt.currentTarget;

            while (lastHoveredSmartSearchCompatibleElement != null &&
                   !(lastHoveredSmartSearchCompatibleElement is Node ||
                     lastHoveredSmartSearchCompatibleElement is FakeNode ||
                     lastHoveredSmartSearchCompatibleElement is Token ||
                     lastHoveredSmartSearchCompatibleElement is Edge ||
                     lastHoveredSmartSearchCompatibleElement is VseGraphView))
            {
                lastHoveredSmartSearchCompatibleElement = lastHoveredSmartSearchCompatibleElement.parent;
            }

            evt.StopPropagation();
        }

        public void EnablePersistedSelectionRestore()
        {
            m_PersistedSelectionRestoreEnabled = true;
        }

        public void DisablePersistedSelectionRestore()
        {
            m_PersistedSelectionRestoreEnabled = false;
        }

        public bool PersistentSelectionContainsElement(GraphElement element)
        {
            if (PersistentSelectionContains != null)
                return PersistentSelectionContains.Invoke(this, element);

            // Missing PersistentSelectionContainsDelegate in GraphView.  Will use reflection

            if (string.IsNullOrEmpty(element.viewDataKey))
                return false;

            FieldInfo persistedSelectionClassType = typeof(GraphView).GetField("m_PersistedSelection", BindingFlags.Instance | BindingFlags.NonPublic);
            object persistedSelection = persistedSelectionClassType?.GetValue(this);

            Type selectionType = persistedSelection?.GetType();
            FieldInfo selectedElementsField = selectionType?.GetField("m_SelectedElements", BindingFlags.Instance | BindingFlags.NonPublic);

            var selectedElements = (HashSet<string>)selectedElementsField?.GetValue(persistedSelection);
            return selectedElements?.Contains(element.viewDataKey) ?? false;
        }

        public void RestoreSelectionForElement(GraphElement element)
        {
            RestorePersistedSelection(this, element);
        }

        internal void AddPlaceholderToken(TokenDeclaration token)
        {
            m_DraggedPlaceholderTokens.Add(token);
        }

        public void ClearPlaceholdersAfterDrag()
        {
            foreach (TokenDeclaration token in m_DraggedPlaceholderTokens)
            {
                UIController.RemoveFromGraphView(token);
            }

            m_DraggedPlaceholderTokens.Clear();
        }

        // TODO: Trunk change to come
        public override void ClearSelection()
        {
            m_Window.ClearNodeInSidePanel();

            if (m_PersistedSelectionRestoreEnabled)
                base.ClearSelection();
            else
                selection.Clear();
        }

        public void NotifyTopologyChange(IGraphModel graphModel)
        {
            viewDataKey = graphModel.GetAssetPath();
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            // Remove all the menu items provided by graph view.
            evt.menu.MenuItems().Clear();

            VseContextualMenuBuilder menuBuilder = new VseContextualMenuBuilder(m_Store, evt, selection, this);
            menuBuilder.BuildContextualMenu();

            evt.StopPropagation();
        }

        bool CanPerformSelectionOperation => selection.OfType<TokenDeclaration>().Any() ||
        selection.OfType<IVisualScriptingField>().Any() ||
        selection.OfType<StickyNote>().Any();

        protected override bool canCopySelection => CanPerformSelectionOperation || base.canCopySelection;
        protected override bool canCutSelection => CanPerformSelectionOperation || base.canCutSelection;
        protected override bool canPaste => CanPerformSelectionOperation || base.canPaste;

        public bool CanCutSelection() => canCutSelection;
        public bool CanCopySelection() => canCopySelection;
        public bool CanPaste() => canPaste;
        public void InvokeCutSelectionCallback() => CutSelectionCallback();
        public void InvokeCopySelectionCallback() => CopySelectionCallback();
        public void InvokePasteCallback() => PasteCallback();

        public void FrameSelectionIfNotVisible()
        {
            if (selection.Cast<VisualElement>().Any(e => !IsElementVisibleInViewport(e)))
                FrameSelection();
        }

        bool IsElementVisibleInViewport(VisualElement element)
        {
            return element != null && element.hierarchy.parent.ChangeCoordinatesTo(this, element.layout).Overlaps(layout);
        }

        public override void AddToSelection(ISelectableGraphElement selectable)
        {
            base.AddToSelection(selectable);

            // m_PersistedSelectionRestoreEnabled check: when clicking on a GO with the same graph while a token/node/
            // ... is selected, GraphView's selection restoration used to set the Selection.activeObject back to the item
            // !m_PersistedSelectionRestoreEnabled implies we're restoring the selection right now and should not set it
            if (m_PersistedSelectionRestoreEnabled &&
                selectable is IHasGraphElementModel hasModel)
            {
                if (!m_ModelProxies.TryGetValue(hasModel.GraphElementModel.GetType(), out var currentProxy))
                {
                    var genericType = typeof(NodeModelProxy<>).MakeGenericType(hasModel.GraphElementModel.GetType());
                    var derivedTypes = TypeCache.GetTypesDerivedFrom(genericType);
                    if (derivedTypes.Any())
                    {
                        var type = derivedTypes.FirstOrDefault();
                        currentProxy = (INodeModelProxy)ScriptableObject.CreateInstance(type);
                        m_ModelProxies.Add(hasModel.GraphElementModel.GetType(), currentProxy);
                    }
                }
                currentProxy?.SetModel(hasModel.GraphElementModel);
                var scriptableObject = currentProxy?.ScriptableObject();
                if (scriptableObject)
                    Selection.activeObject = scriptableObject;
            }

            m_Window.ShowNodeInSidePanel(selectable, true);

            // TODO: convince UIElements to add proper support for Z Order as this won't survive a refresh
            // alternative: reorder models or store our own z order in models
            if (selectable is VisualElement visualElement)
            {
                if (visualElement is Token)
                    visualElement.BringToFront();
                else if (visualElement is Node node)
                {
                    node.BringToFront();
                }
            }

            OnSelectionChangedCallback?.Invoke(selection);
        }

        public override void RemoveFromSelection(ISelectableGraphElement selectable)
        {
            base.RemoveFromSelection(selectable);
            m_Window.ShowNodeInSidePanel(selectable, false);
            OnSelectionChangedCallback?.Invoke(selection);
        }

        internal override void OnViewDataReady()
        {
            base.OnViewDataReady();

            if (selection.OfType<Node>().Any())
            {
                m_Window.ShowNodeInSidePanel(selection.OfType<Node>().Last(), true);
            }
            else
            {
                m_Window.ShowNodeInSidePanel(null, false);
            }
        }

        internal PositionDependenciesManager PositionDependenciesManagers;
        IExternalDragNDropHandler m_CurrentDragNDropHandler;

        public void AddPositionDependency(IEdgeModel model)
        {
            PositionDependenciesManagers.AddPositionDependency(model);
        }

        public void RemovePositionDependency(IEdgeModel model)
        {
            PositionDependenciesManagers.Remove(model.OutputNodeGuid, model.InputNodeGuid);
            PositionDependenciesManagers.LogDependencies();
        }

        public void AddPortalDependency(IEdgePortalModel model)
        {
            PositionDependenciesManagers.AddPortalDependency(model);
        }

        public void RemovePortalDependency(IEdgePortalModel model)
        {
            PositionDependenciesManagers.RemovePortalDependency(model);
            PositionDependenciesManagers.LogDependencies();
        }

        public EventPropagation AlignSelection(bool follow)
        {
            PositionDependenciesManagers.AlignNodes(this, follow, selection);
            return EventPropagation.Stop;
        }

        public void AlignGraphElements(IEnumerable<GraphElement> entryPoints)
        {
            PositionDependenciesManagers.AlignNodes(this, true, entryPoints.OfType<ISelectableGraphElement>().ToList());
        }

        public void PanToNode(GUID nodeGuid)
        {
            var graphModel = m_Store.GetState().CurrentGraphModel;

            if (!graphModel.NodesByGuid.TryGetValue(nodeGuid, out var nodeModel))
                return;

            GraphElement graphElement = this.Query<Node>().Where(e => ReferenceEquals(e.NodeModel, nodeModel)).First();
            if (graphElement == null)
                graphElement = this.Query<Token>().Where(e => ReferenceEquals(e.GraphElementModel, nodeModel)).First();

            if (graphElement == null)
                return;

            graphElement.Select(this, false);
            FrameSelection();
        }

        public override GraphElements.Blackboard GetBlackboard()
        {
            return UIController.Blackboard;
        }

        protected override PlacematContainer CreatePlacematContainer()
        {
            return new PlacematContainer(this);
        }
    }
}
