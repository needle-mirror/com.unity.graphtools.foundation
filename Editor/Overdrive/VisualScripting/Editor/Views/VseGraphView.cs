using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.SmartSearch;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public class VseGraphView : GraphView, IDropTarget
    {
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

        DebugDisplayElement ContentDebugDisplay { get; }

        public bool ShowDebug { get; set; }

        public VseUIController UIController { get; }

        public static GraphElement clickTarget;
        public static double clickTimeSinceStartupSecs;

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

                    GraphElement elem = (GraphElement)selection.FirstOrDefault(x => x is IGraphElement hasModel && hasModel.Model is IGTFNodeModel);
                    if (elem == null)
                        return;

                    IGTFNodeModel elemModel = (IGTFNodeModel)elem.Model;
                    Vector2 elemPos = elemModel.Position;
                    Vector2 startPos = contentViewContainer.ChangeCoordinatesTo(elem.hierarchy.parent, elemPos);

                    bool requireShiftToMoveDependencies = !(elemModel.GraphModel?.Stencil?.MoveNodeDependenciesByDefault).GetValueOrDefault();
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
            Selection.activeObject = m_Store.GetState()?.CurrentGraphModel.AssetModel as Object;
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
                .Select(x => x.Model)
                .ToList());
            s_LastCopiedData = copyPasteData;
            return copyPasteData.IsEmpty() ? string.Empty : "data";// copyPasteData.ToJson();
        }

        internal static CopyPasteData GatherCopiedElementsData(IReadOnlyCollection<IGTFGraphElementModel> graphElementModels)
        {
            IEnumerable<NodeModel> originalNodes = graphElementModels.OfType<NodeModel>();
            List<NodeModel> nodesToCopy = originalNodes
                .ToList();

            IEnumerable<IGTFNodeModel> floatingNodes = graphElementModels.OfType<IGTFNodeModel>();

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

        internal static void OnUnserializeAndPaste(IGTFGraphModel graph, TargetInsertionInfo targetInfo, IGTFEditorDataModel editorDataModel, CopyPasteData data)
        {
            CopyPasteData copyPasteData = data;

            CreateElementsFromCopiedData(graph, targetInfo, editorDataModel, copyPasteData);
        }

        static void CreateElementsFromCopiedData(IGTFGraphModel graph, TargetInsertionInfo targetInfo,
            IGTFEditorDataModel editorDataModel,
            CopyPasteData copyPasteData)
        {
            var elementMapping = new Dictionary<string, IGTFGraphElementModel>();

            var nodeMapping = new Dictionary<IGTFNodeModel, IGTFNodeModel>();
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

                IGTFPortModel inputPortModel = null;
                IGTFPortModel outputPortModel = null;
                if (newInput != null && newOutput != null)
                {
                    // Both node were duplicated; create a new edge between the duplicated nodes.
                    inputPortModel = (newInput as IGTFNodeModel)?.InputsById[edge.ToPortId];
                    outputPortModel = (newOutput as IGTFNodeModel)?.OutputsById[edge.FromPortId];
                }
                else if (newInput != null)
                {
                    inputPortModel = (newInput as IGTFNodeModel)?.InputsById[edge.ToPortId];
                    outputPortModel = edge.FromPort;
                }
                else if (newOutput != null)
                {
                    inputPortModel = edge.ToPort;
                    outputPortModel = (newOutput as IGTFNodeModel)?.OutputsById[edge.FromPortId];
                }

                if (inputPortModel != null && outputPortModel != null)
                {
                    if (inputPortModel.Capacity == PortCapacity.Single && inputPortModel.ConnectedEdges.Any())
                        continue;
                    if (outputPortModel.Capacity == PortCapacity.Single && outputPortModel.ConnectedEdges.Any())
                        continue;

                    var copiedEdge = graph.CreateEdge(inputPortModel, outputPortModel);
                    elementMapping.Add(edge.GetId(), copiedEdge);
                    editorDataModel?.SelectElementsUponCreation(new[] { copiedEdge }, true);
                }
            }

            if (copyPasteData.variableDeclarations.Any())
            {
                List<IGTFVariableDeclarationModel> variableDeclarationModels =
                    copyPasteData.variableDeclarations.Cast<IGTFVariableDeclarationModel>().ToList();

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
                        IGTFGraphElementModel pastedElement;
                        if (elementMapping.TryGetValue(guid, out pastedElement))
                        {
                            pastedHiddenContent.Add(pastedElement.Guid.ToString());
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

            var graph = m_Store.GetState().CurrentGraphModel;
            CopyPasteData copyPasteData = s_LastCopiedData;// JsonUtility.FromJson<CopyPasteData>(data);
            var delta = m_LastMousePosition - copyPasteData.topLeftNodePosition;

            TargetInsertionInfo info;
            info.OperationName = operationName;
            info.Delta = delta;

            IGTFEditorDataModel editorDataModel = m_Store.GetState().EditorDataModel;
            m_Store.Dispatch(new PasteSerializedDataAction(graph, info, editorDataModel, s_LastCopiedData));
        }

        static void PasteNode(string operationName, IGTFNodeModel copiedNode, Dictionary<IGTFNodeModel, IGTFNodeModel> mapping,
            IGTFGraphModel graph, IGTFEditorDataModel editorDataModel, Vector2 delta)
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

            List<(IGTFVariableDeclarationModel, SerializableGUID, Vector2)> variablesToCreate = DragAndDropHelper.ExtractVariablesFromDroppedElements(dropElements, this, evt.mousePosition);

            List<Node> droppedNodes = dropElements.OfType<Node>().ToList();

            if (droppedNodes.Any(e => !(e.NodeModel is IGTFVariableNodeModel)) && variablesToCreate.Any())
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

        public EventPropagation RemoveSelection()
        {
            IGTFNodeModel[] selectedNodes = selection.OfType<IGraphElement>()
                .Select(x => x.Model).OfType<IGTFNodeModel>().ToArray();

            IGTFNodeModel[] connectedNodes = selectedNodes.Where(x => x.InputsById.Values
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
                     lastHoveredVisualElement is Token ||
                     lastHoveredVisualElement is VseGraphView ||
                     lastHoveredVisualElement.GetFirstOfType<TokenDeclaration>() != null))
            {
                lastHoveredVisualElement = lastHoveredVisualElement.parent;
            }

            lastHoveredSmartSearchCompatibleElement = (VisualElement)evt.currentTarget;

            while (lastHoveredSmartSearchCompatibleElement != null &&
                   !(lastHoveredSmartSearchCompatibleElement is Node ||
                     lastHoveredSmartSearchCompatibleElement is Token ||
                     lastHoveredSmartSearchCompatibleElement is Edge ||
                     lastHoveredSmartSearchCompatibleElement is VseGraphView))
            {
                lastHoveredSmartSearchCompatibleElement = lastHoveredSmartSearchCompatibleElement.parent;
            }

            evt.StopPropagation();
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

        public override void ClearSelection()
        {
            m_Window.ClearNodeInSidePanel();
            base.ClearSelection();
        }

        public void NotifyTopologyChange(IGTFGraphModel graphModel)
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

        public void AddPositionDependency(IGTFEdgeModel model)
        {
            PositionDependenciesManagers.AddPositionDependency(model);
        }

        public void RemovePositionDependency(IGTFEdgeModel model)
        {
            if (model is IEdgeModel edgeModel)
            {
                PositionDependenciesManagers.Remove(edgeModel.FromNodeGuid, edgeModel.ToNodeGuid);
                PositionDependenciesManagers.LogDependencies();
            }
        }

        public void AddPortalDependency(IGTFEdgePortalModel model)
        {
            PositionDependenciesManagers.AddPortalDependency(model);
        }

        public void RemovePortalDependency(IGTFEdgePortalModel model)
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

            var graphElement = nodeModel.GetUI(this);
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

        internal void DisplayTokenDeclarationSearcher(IGTFVariableDeclarationModel declaration, Vector2 pos)
        {
            if (store.GetState().CurrentGraphModel == null || !(declaration is VariableDeclarationModel vdm))
            {
                return;
            }

            SearcherService.ShowTypes(
                store.GetState().CurrentGraphModel.Stencil,
                pos,
                (t, i) =>
                {
                    var graphModel = store.GetState().CurrentGraphModel;
                    vdm.DataType = t;

                    foreach (var usage in graphModel.FindReferencesInGraph<VariableNodeModel>(vdm))
                    {
                        usage.UpdateTypeFromDeclaration();
                    }

                    store.Dispatch(new RefreshUIAction(UpdateFlags.All));
                });
        }

        public override IEnumerable<IHighlightable> Highlightables
        {
            get
            {
                IEnumerable<IHighlightable> elements = graphElements.ToList().OfType<IHighlightable>();
                return elements.Concat(UIController.Blackboard?.GraphVariables ?? Enumerable.Empty<IHighlightable>());
            }
        }
    }
}
