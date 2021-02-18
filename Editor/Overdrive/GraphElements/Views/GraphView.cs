using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// The <see cref="VisualElement"/> in which graphs are drawn.
    /// </summary>
    public class GraphView : GraphViewBridge, ISelection, IDragAndDropHandler
    {
        public class Layer : VisualElement {}

        class ContentViewContainer : VisualElement
        {
            public override bool Overlaps(Rect r)
            {
                return true;
            }
        }

        [Serializable]
        class PersistedViewTransform
        {
            public Vector3 position = Vector3.zero;
            public Vector3 scale = Vector3.one;
        }

        enum FrameType
        {
            All = 0,
            Selection = 1,
            Origin = 2
        }

        public delegate void ViewTransformChanged(GraphView graphView);
        public delegate string SerializeGraphElementsDelegate(IEnumerable<GraphElement> elements);
        public delegate bool CanPasteSerializedDataDelegate(string data);
        public delegate void UnserializeAndPasteDelegate(string operationName, string data);

        public event Action<List<ISelectableGraphElement>> OnSelectionChangedCallback;

        public const float DragDropSpacer = 5f;

        public static readonly string ussClassName = "ge-graph-view";

        const string k_SelectionUndoRedoLabel = "Change GraphView Selection";
        const int k_FrameBorder = 30;
        const string k_SerializedDataMimeType = "application/vnd.unity.graphview.elements";

        int m_SavedSelectionVersion;

        PersistedSelection m_PersistedSelection;

        GraphViewUndoRedoSelection m_GraphViewUndoRedoSelection;

        readonly Dictionary<int, Layer> m_ContainerLayers = new Dictionary<int, Layer>();

        IVisualElementScheduledItem m_OnTimerTicker;

        PersistedViewTransform m_PersistedViewTransform;

        UQueryState<GraphElement> m_AllGraphElements;

        ContextualMenuManipulator m_ContextualMenuManipulator;
        ContentZoomer m_Zoomer;
        ShortcutHandler m_ShortcutHandler;

        AutoSpacingHelper m_AutoSpacingHelper;
        AutoAlignmentHelper m_AutoAlignmentHelper;

        float m_MinScale = ContentZoomer.DefaultMinScale;
        float m_MaxScale = ContentZoomer.DefaultMaxScale;
        float m_MaxScaleOnFrame = 1.0f;
        float m_ScaleStep = ContentZoomer.DefaultScaleStep;
        float m_ReferenceScale = ContentZoomer.DefaultReferenceScale;

        Blackboard m_Blackboard;
        readonly VisualElement m_GraphViewContainer;
        readonly VisualElement m_BadgesParent;

        SelectionDragger m_SelectionDragger;
        ContentDragger m_ContentDragger;
        Clickable m_Clickable;
        RectangleSelector m_RectangleSelector;
        FreehandSelector m_FreehandSelector;

        // Cache for INodeModelProxies
        Dictionary<Type, INodeModelProxy> m_ModelProxies;

        string m_Clipboard = string.Empty;

        IDragAndDropHandler m_CurrentDragAndDropHandler;
        BlackboardDragAndDropHandler m_BlackboardDragAndDropHandler;

        protected bool m_SelectionDraggerWasActive;
        protected Vector2 m_LastMousePosition;

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

        protected ContextualMenuManipulator ContextualMenuManipulator
        {
            get => m_ContextualMenuManipulator;
            set => this.ReplaceManipulator(ref m_ContextualMenuManipulator, value);
        }

        protected ContentZoomer ContentZoomer
        {
            get => m_Zoomer;
            set => this.ReplaceManipulator(ref m_Zoomer, value);
        }

        public ShortcutHandler ShortcutHandler
        {
            get => m_ShortcutHandler;
            set => this.ReplaceManipulator(ref m_ShortcutHandler, value);
        }

        public CommandDispatcher CommandDispatcher { get; }

        public GraphViewEditorWindow Window { get; }

        public UQueryState<GraphElement> GraphElements { get; }

        public UQueryState<Node> Nodes { get; }

        public UQueryState<Port> Ports { get; }

        public UQueryState<Edge> Edges { get; }

        public UQueryState<StickyNote> Stickies { get; }

        public ViewTransformChanged ViewTransformChangedCallback { get; set; }

        public virtual bool SupportsWindowedBlackboard => false;

        public override VisualElement contentContainer => m_GraphViewContainer; // Contains full content, potentially partially visible

        public PlacematContainer PlacematContainer { get; }

        public List<ISelectableGraphElement> Selection { get; }

        public virtual bool CanCopySelection => Selection.OfType<GraphElement>().Any(ge => ge.IsCopiable());

        public virtual bool CanCutSelection => Selection.Any(s => s is Node || s is Placemat);

        public virtual bool CanPaste => CanPasteSerializedData(Clipboard);

        public virtual bool CanDuplicateSelection => CanCopySelection;

        public virtual bool CanDeleteSelection => Selection.OfType<GraphElement>().Any(e => e.IsDeletable());

        public SerializeGraphElementsDelegate SerializeGraphElementsCallback { get; set; }

        public CanPasteSerializedDataDelegate CanPasteSerializedDataCallback { get; set; }

        public UnserializeAndPasteDelegate UnserializeAndPasteCallback { get; set; }

        public virtual IEnumerable<IHighlightable> Highlightables
        {
            get
            {
                var elements = GraphElements.ToList().OfType<IHighlightable>();

                // PF: FIX this comment by @joce
                // This assumes that:
                //
                // The blackboard will never again be part of the GraphView (not a guarantee)
                // The blackboard will only ever have GraphVariables that will be highlightable
                //
                // To address this, I think you'd need to query all the elements of the blackboard that
                // are highlightable (similar to the GraphElements.ToList().OfType<IHighlightable> above)
                // and add a Distinct after the Concat call.
                //
                // We could add a Highlightables property to the Blackboard.
                return elements.Concat(GetBlackboard()?.Highlightables ?? Enumerable.Empty<IHighlightable>());
            }
        }

        // For tests only
        internal bool UseInternalClipboard { get; set; }

        // Internal access for tests.
        internal string Clipboard
        {
            get => UseInternalClipboard ? m_Clipboard : EditorGUIUtility.systemCopyBuffer;

            set
            {
                if (UseInternalClipboard)
                {
                    m_Clipboard = value;
                }
                else
                {
                    EditorGUIUtility.systemCopyBuffer = value;
                }
            }
        }

        public Blackboard GetBlackboard()
        {
            if (m_Blackboard == null && CommandDispatcher?.GraphToolState?.BlackboardGraphModel != null)
            {
                m_Blackboard = GraphElementFactory.CreateUI<Blackboard>(this, CommandDispatcher, CommandDispatcher.GraphToolState.BlackboardGraphModel);
                m_Blackboard?.AddToGraphView(this);
            }

            return m_Blackboard;
        }

        internal PositionDependenciesManager PositionDependenciesManager { get; }

        public GraphView(GraphViewEditorWindow window, CommandDispatcher commandDispatcher, string uniqueGraphViewName = null)
        {
            if (uniqueGraphViewName == null)
                uniqueGraphViewName = "GraphView_" + new Random().Next();

            focusable = true;

            // This is needed for selection persistence.
            viewDataKey = uniqueGraphViewName;
            name = uniqueGraphViewName;

            Window = window;
            CommandDispatcher = commandDispatcher;

            AddToClassList(ussClassName);

            this.SetRenderHintsForGraphView();

            Selection = new List<ISelectableGraphElement>();

            m_GraphViewContainer = new VisualElement() {name = "graph-view-container"};
            m_GraphViewContainer.pickingMode = PickingMode.Ignore;
            hierarchy.Add(m_GraphViewContainer);

            contentViewContainer = new ContentViewContainer
            {
                name = "content-view-container",
                pickingMode = PickingMode.Ignore,
                usageHints = UsageHints.GroupTransform
            };

            // make it absolute and 0 sized so it acts as a transform to move children to and fro
            m_GraphViewContainer.Add(contentViewContainer);

            m_BadgesParent = new VisualElement { name = "badge-container"};

            this.AddStylesheet("GraphView.uss");

            GraphElements = contentViewContainer.Query<GraphElement>().Build();
            m_AllGraphElements = this.Query<GraphElement>().Build();
            Nodes = contentViewContainer.Query<Node>().Build();
            Edges = this.Query<Layer>().Children<Edge>().Build();
            Stickies = this.Query<Layer>().Children<StickyNote>().Build();
            Ports = contentViewContainer.Query().Children<Layer>().Descendents<Port>().Build();

            PositionDependenciesManager = new PositionDependenciesManager(this, CommandDispatcher?.GraphToolState?.Preferences);
            m_AutoAlignmentHelper = new AutoAlignmentHelper(this);
            m_AutoSpacingHelper = new AutoSpacingHelper(this);

            m_ModelProxies = new Dictionary<Type, INodeModelProxy>();

            ContextualMenuManipulator = new ContextualMenuManipulator(BuildContextualMenu);

            Clickable = new Clickable(OnDoubleClick);
            Clickable.activators.Clear();
            Clickable.activators.Add(
                new ManipulatorActivationFilter { button = MouseButton.LeftMouse, clickCount = 2 });

            ContentDragger = new ContentDragger();
            SelectionDragger = new SelectionDragger(this);
            RectangleSelector = new RectangleSelector();
            FreehandSelector = new FreehandSelector();

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale, 1.0f);

            RegisterCallback<ValidateCommandEvent>(OnValidateCommand);
            RegisterCallback<ExecuteCommandEvent>(OnExecuteCommand);
            RegisterCallback<AttachToPanelEvent>(OnEnterPanel);
            RegisterCallback<DetachFromPanelEvent>(OnLeavePanel);

            RegisterCallback<MouseOverEvent>(OnMouseOver);
            RegisterCallback<MouseMoveEvent>(OnMouseMove);

            // TODO: Until GraphView.SelectionDragger is used widely in VS, we must register to drag events ON TOP of
            // using the VisualScripting.Editor.SelectionDropper, just to deal with drags from the Blackboard
            RegisterCallback<DragEnterEvent>(OnDragEnter);
            RegisterCallback<DragLeaveEvent>(OnDragLeave);
            RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
            RegisterCallback<DragExitedEvent>(OnDragExited);
            RegisterCallback<DragPerformEvent>(OnDragPerform);
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);

            Insert(0, new GridBackground());

            PlacematContainer = new PlacematContainer(this);
            AddLayer(PlacematContainer, PlacematContainer.PlacematsLayer);

            SerializeGraphElementsCallback = OnSerializeGraphElements;
            UnserializeAndPasteCallback = UnserializeAndPaste;
        }

        internal void UnloadGraph()
        {
            GetBlackboard()?.Clear();
            ClearGraph();
        }

        void ClearGraph()
        {
            List<GraphElement> elements = GraphElements.ToList();

            PositionDependenciesManager.Clear();
            foreach (var element in elements)
            {
                RemoveElement(element);
            }
        }

        public void UpdateViewTransform(Vector3 newPosition, Vector3 newScale)
        {
            float validateFloat = newPosition.x + newPosition.y + newPosition.z + newScale.x + newScale.y + newScale.z;
            if (float.IsInfinity(validateFloat) || float.IsNaN(validateFloat))
                return;

            newPosition.x = GraphViewStaticBridge.RoundToPixelGrid(newPosition.x);
            newPosition.y = GraphViewStaticBridge.RoundToPixelGrid(newPosition.y);

            contentViewContainer.transform.position = newPosition;
            contentViewContainer.transform.scale = newScale;

            UpdatePersistedViewTransform();

            ViewTransformChangedCallback?.Invoke(this);
        }

        protected virtual BlackboardDragAndDropHandler GetBlackboardDragAndDropHandler()
        {
            return m_BlackboardDragAndDropHandler ??
                (m_BlackboardDragAndDropHandler = new BlackboardDragAndDropHandler(this));
        }

        /// <summary>
        /// Find an appropriate Drag-and-drop handler for the DragEnter event
        /// </summary>
        /// <param name="evt">current DragEnter event</param>
        /// <param name="graphView">graphview where the event happens</param>
        /// <returns>handler or null if nothing appropriate was found</returns>
        protected virtual IDragAndDropHandler GetExternalDragNDropHandler(DragEnterEvent evt)
        {
            var blackboardDragAndDropHandler = GetBlackboardDragAndDropHandler();
            if (DragAndDrop.objectReferences.Length == 0 && Selection.OfType<BlackboardField>().Any())
            {
                return blackboardDragAndDropHandler;
            }

            return null;
        }

        public bool GetPortCenterOverride(Port port, out Vector2 overriddenPosition)
        {
            if (PlacematContainer.GetPortCenterOverride(port, out overriddenPosition))
                return true;

            overriddenPosition = Vector3.zero;
            return false;
        }

        void ClearSavedSelection()
        {
            if (m_PersistedSelection == null)
                return;

            m_PersistedSelection.SelectedElements.Clear();
            SaveViewData();
        }

        bool ShouldRecordUndo()
        {
            return m_GraphViewUndoRedoSelection != null &&
                m_PersistedSelection != null &&
                m_SavedSelectionVersion == m_GraphViewUndoRedoSelection.Version;
        }

        void RestoreSavedSelection(IGraphViewSelection graphViewSelection)
        {
            if (m_PersistedSelection == null)
                return;
            if (graphViewSelection.SelectedElements.Count == Selection.Count && graphViewSelection.Version == m_SavedSelectionVersion)
                return;

            // Update both selection objects' versions.
            m_GraphViewUndoRedoSelection.Version = graphViewSelection.Version;
            m_PersistedSelection.Version = graphViewSelection.Version;

            ClearSelectionNoUndoRecord();
            foreach (string guid in graphViewSelection.SelectedElements)
            {
                var element = GetElementByGuid(guid);
                if (element == null)
                    continue;

                AddToSelectionNoUndoRecord(element);
            }

            m_SavedSelectionVersion = graphViewSelection.Version;

            IGraphViewSelection selectionObjectToSync = m_GraphViewUndoRedoSelection;
            if (graphViewSelection is GraphViewUndoRedoSelection)
                selectionObjectToSync = m_PersistedSelection;

            selectionObjectToSync.SelectedElements.Clear();

            foreach (string guid in graphViewSelection.SelectedElements)
            {
                selectionObjectToSync.SelectedElements.Add(guid);
            }
        }

        void RestorePersistentSelectionForElement(GraphElement element)
        {
            if (m_PersistedSelection == null)
                return;

            if (m_PersistedSelection.SelectedElements.Count == Selection.Count && m_PersistedSelection.Version == m_SavedSelectionVersion)
                return;

            if (string.IsNullOrEmpty(element.viewDataKey))
                return;

            if (m_PersistedSelection.SelectedElements.Contains(element.viewDataKey))
            {
                AddToSelectionNoUndoRecord(element);
            }
        }

        void UndoRedoPerformed()
        {
            RestoreSavedSelection(m_GraphViewUndoRedoSelection);
        }

        void RecordSelectionUndoPre()
        {
            if (m_GraphViewUndoRedoSelection == null)
                return;

            Undo.RegisterCompleteObjectUndo(m_GraphViewUndoRedoSelection, k_SelectionUndoRedoLabel);
        }

        void RecordSelectionUndoPost()
        {
            m_GraphViewUndoRedoSelection.Version++;
            m_SavedSelectionVersion = m_GraphViewUndoRedoSelection.Version;

            m_PersistedSelection.Version++;

            if (m_OnTimerTicker == null)
            {
                m_OnTimerTicker = schedule.Execute(DelayPersistentDataSave);
            }

            m_OnTimerTicker.ExecuteLater(1);
        }

        void DelayPersistentDataSave()
        {
            m_OnTimerTicker = null;
            SaveViewData();
        }

        void AddLayer(Layer layer, int index)
        {
            m_ContainerLayers.Add(index, layer);

            int indexOfLayer = m_ContainerLayers.OrderBy(t => t.Key).Select(t => t.Value).ToList().IndexOf(layer);

            contentViewContainer.Insert(indexOfLayer, layer);
        }

        void AddLayer(int index)
        {
            Layer layer = new Layer { pickingMode = PickingMode.Ignore };
            AddLayer(layer, index);
        }

        VisualElement GetLayer(int index)
        {
            return m_ContainerLayers[index];
        }

        internal void ChangeLayer(GraphElement element)
        {
            if (!m_ContainerLayers.ContainsKey(element.Layer))
                AddLayer(element.Layer);

            bool selected = element.Selected;
            if (selected)
                element.UnregisterCallback<DetachFromPanelEvent>(OnSelectedElementDetachedFromPanel);

            GetLayer(element.Layer).Add(element);

            if (selected)
                element.RegisterCallback<DetachFromPanelEvent>(OnSelectedElementDetachedFromPanel);
        }

        GraphElement GetElementByGuid(string guid)
        {
            return m_AllGraphElements.ToList().FirstOrDefault(e => e.viewDataKey == guid);
        }

        public void SetupZoom(float minScaleSetup, float maxScaleSetup, float maxScaleOnFrame)
        {
            SetupZoom(minScaleSetup, maxScaleSetup, maxScaleOnFrame, m_ScaleStep, m_ReferenceScale);
        }

        public void SetupZoom(float minScaleSetup, float maxScaleSetup, float maxScaleOnFrame, float scaleStepSetup, float referenceScaleSetup)
        {
            m_MinScale = minScaleSetup;
            m_MaxScale = maxScaleSetup;
            m_MaxScaleOnFrame = maxScaleOnFrame;
            m_ScaleStep = scaleStepSetup;
            m_ReferenceScale = referenceScaleSetup;
            UpdateContentZoomer();
        }

        void UpdatePersistedViewTransform()
        {
            if (m_PersistedViewTransform == null)
                return;

            m_PersistedViewTransform.position = contentViewContainer.transform.position;
            m_PersistedViewTransform.scale = contentViewContainer.transform.scale;

            SaveViewData();
        }

        internal override void OnViewDataReady()
        {
            base.OnViewDataReady();

            string key = GetFullHierarchicalViewDataKey();

            m_PersistedViewTransform = GetOrCreateViewData<PersistedViewTransform>(m_PersistedViewTransform, key);

            m_PersistedSelection = GetOrCreateViewData<PersistedSelection>(m_PersistedSelection, key);

            UpdateViewTransform(m_PersistedViewTransform.position, m_PersistedViewTransform.scale);
            RestoreSavedSelection(m_PersistedSelection);

            if (Window != null)
            {
                if (Selection.OfType<Node>().Any())
                {
                    Window.ShowNodeInSidePanel(Selection.OfType<Node>().Last(), true);
                }
                else
                {
                    Window.ShowNodeInSidePanel(null, false);
                }
            }
        }

        void UpdateContentZoomer()
        {
            if (Math.Abs(m_MinScale - m_MaxScale) > float.Epsilon)
            {
                ContentZoomer = new ContentZoomer
                {
                    minScale = m_MinScale,
                    maxScale = m_MaxScale,
                    scaleStep = m_ScaleStep,
                    referenceScale = m_ReferenceScale
                };
            }
            else
            {
                ContentZoomer = null;
            }

            ValidateTransform();
        }

        void ValidateTransform()
        {
            if (contentViewContainer == null)
                return;
            Vector3 transformScale = viewTransform.scale;

            transformScale.x = Mathf.Clamp(transformScale.x, m_MinScale, m_MaxScale);
            transformScale.y = Mathf.Clamp(transformScale.y, m_MinScale, m_MaxScale);

            viewTransform.scale = transformScale;
        }

        // functions to ISelection extensions
        public virtual void AddToSelection(ISelectableGraphElement selectable)
        {
            var graphElement = selectable as GraphElement;
            if (graphElement == null)
                return;

            if (Selection.Contains(selectable))
                return;

            AddToSelectionNoUndoRecord(graphElement);

            if (ShouldRecordUndo())
            {
                RecordSelectionUndoPre();
                m_GraphViewUndoRedoSelection.SelectedElements.Add(graphElement.viewDataKey);
                m_PersistedSelection.SelectedElements.Add(graphElement.viewDataKey);
                RecordSelectionUndoPost();
            }

            // m_PersistedSelectionRestoreEnabled check: when clicking on a GO with the same graph while a token/node/
            // ... is selected, GraphView's selection restoration used to set the Selection.activeObject back to the item
            // !m_PersistedSelectionRestoreEnabled implies we're restoring the selection right now and should not set it
            if (selectable is IModelUI hasModel)
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

            if (Window != null)
            {
                Window.ShowNodeInSidePanel(selectable, true);
            }

            // TODO: convince UIElements to add proper support for Z Order as this won't survive a refresh
            // alternative: reorder models or store our own z order in models
            if (selectable is VisualElement visualElement)
            {
                if (visualElement is Node)
                    visualElement.BringToFront();
            }

            OnSelectionChangedCallback?.Invoke(Selection);
        }

        void AddToSelectionNoUndoRecord(GraphElement graphElement)
        {
            graphElement.Selected = true;
            Selection.Add(graphElement);
            graphElement.OnSelected();

            // To ensure that the selected GraphElement gets unselected if it is removed from the GraphView.
            graphElement.RegisterCallback<DetachFromPanelEvent>(OnSelectedElementDetachedFromPanel);

            this.HighlightGraphElements();

            graphElement.MarkDirtyRepaint();
        }

        void RemoveFromSelectionNoUndoRecord(ISelectableGraphElement selectable)
        {
            var graphElement = selectable as GraphElement;
            if (graphElement == null)
                return;
            graphElement.Selected = false;

            Selection.Remove(selectable);
            graphElement.OnUnselected();
            graphElement.UnregisterCallback<DetachFromPanelEvent>(OnSelectedElementDetachedFromPanel);

            this.HighlightGraphElements();

            graphElement.MarkDirtyRepaint();
        }

        public virtual void RemoveFromSelection(ISelectableGraphElement selectable)
        {
            var graphElement = selectable as GraphElement;
            if (graphElement == null)
                return;

            if (!Selection.Contains(selectable))
                return;

            RemoveFromSelectionNoUndoRecord(selectable);

            if (ShouldRecordUndo())
            {
                RecordSelectionUndoPre();
                m_GraphViewUndoRedoSelection.SelectedElements.Remove(graphElement.viewDataKey);
                m_PersistedSelection.SelectedElements.Remove(graphElement.viewDataKey);
                RecordSelectionUndoPost();
            }

            if (Window != null)
            {
                Window.ShowNodeInSidePanel(selectable, false);
            }

            OnSelectionChangedCallback?.Invoke(Selection);
        }

        bool ClearSelectionNoUndoRecord()
        {
            foreach (var graphElement in Selection.OfType<GraphElement>())
            {
                graphElement.Selected = false;

                graphElement.OnUnselected();
                graphElement.UnregisterCallback<DetachFromPanelEvent>(OnSelectedElementDetachedFromPanel);
                graphElement.MarkDirtyRepaint();
            }

            bool selectionWasNotEmpty = Selection.Any();
            Selection.Clear();

            this.HighlightGraphElements();

            return selectionWasNotEmpty;
        }

        public virtual void ClearSelection()
        {
            Window.ClearNodeInSidePanel();

            bool selectionWasNotEmpty = ClearSelectionNoUndoRecord();

            if (ShouldRecordUndo() && selectionWasNotEmpty)
            {
                RecordSelectionUndoPre();
                m_GraphViewUndoRedoSelection.SelectedElements.Clear();
                m_PersistedSelection.SelectedElements.Clear();
                RecordSelectionUndoPost();
            }

            UnityEditor.Selection.activeObject = null;
        }

        void OnSelectedElementDetachedFromPanel(DetachFromPanelEvent evt)
        {
            RemoveFromSelectionNoUndoRecord(evt.target as ISelectableGraphElement);
        }

        protected virtual void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (CommandDispatcher == null)
                return;

            if (evt.menu.MenuItems().Count > 0)
                evt.menu.AppendSeparator();

            evt.menu.AppendAction("Create Node", menuAction =>
            {
                Vector2 mousePosition = menuAction?.eventInfo?.mousePosition ?? Event.current.mousePosition;
                DisplaySmartSearch(mousePosition);
            });

            evt.menu.AppendAction("Create Placemat", menuAction =>
            {
                Vector2 mousePosition = menuAction?.eventInfo?.mousePosition ?? Event.current.mousePosition;
                Vector2 graphPosition = contentViewContainer.WorldToLocal(mousePosition);

                CommandDispatcher.Dispatch(new CreatePlacematCommand(new Rect(graphPosition.x, graphPosition.y, 200, 200)));
            });

            if (Selection.Any())
            {
                var nodesAndNotes = Selection.OfType<GraphElement>().Where(e => (e is Node || e is StickyNote)).ToList();
                evt.menu.AppendAction("Create Placemat Under Selection", _ =>
                {
                    Rect bounds = new Rect();
                    if (Placemat.ComputeElementBounds(ref bounds, nodesAndNotes))
                    {
                        CommandDispatcher.Dispatch(new CreatePlacematCommand(bounds));
                    }
                }, nodesAndNotes.Count == 0 ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);

                /* Actions on selection */

                evt.menu.AppendSeparator();

                evt.menu.AppendAction("Align Elements/Align Items", _ =>
                {
                    CommandDispatcher.Dispatch(new AlignNodesCommand(this, false));
                });

                evt.menu.AppendAction("Align Elements/Align Hierarchy", _ =>
                {
                    CommandDispatcher.Dispatch(new AlignNodesCommand(this, true));
                });

                if (Selection.OfType<GraphElement>().Count(elem => !(elem is Edge) && elem.visible) > 1)
                {
                    evt.menu.AppendAction("Align Elements/Top",
                        _ => m_AutoAlignmentHelper.SendAlignCommand(AutoAlignmentHelper.AlignmentReference.Top));

                    evt.menu.AppendAction("Align Elements/Bottom",
                        _ => m_AutoAlignmentHelper.SendAlignCommand(AutoAlignmentHelper.AlignmentReference.Bottom));

                    evt.menu.AppendAction("Align Elements/Left",
                        _ => m_AutoAlignmentHelper.SendAlignCommand(AutoAlignmentHelper.AlignmentReference.Left));

                    evt.menu.AppendAction("Align Elements/Right",
                        _ => m_AutoAlignmentHelper.SendAlignCommand(AutoAlignmentHelper.AlignmentReference.Right));

                    evt.menu.AppendAction("Align Elements/Horizontal Center",
                        _ => m_AutoAlignmentHelper.SendAlignCommand(AutoAlignmentHelper.AlignmentReference.HorizontalCenter));

                    evt.menu.AppendAction("Align Elements/Vertical Center",
                        _ => m_AutoAlignmentHelper.SendAlignCommand(AutoAlignmentHelper.AlignmentReference.VerticalCenter));

                    evt.menu.AppendAction("Space Elements/Horizontal",
                        _ => m_AutoSpacingHelper.SendSpacingCommand(Orientation.Horizontal));

                    evt.menu.AppendAction("Space Elements/Vertical",
                        _ => m_AutoSpacingHelper.SendSpacingCommand(Orientation.Vertical));
                }

                var nodes = Selection.OfType<Node>().Select(e => e.NodeModel).ToArray();
                if (nodes.Length > 0)
                {
                    var connectedNodes = nodes
                        .Where(m => m.GetConnectedEdges().Any())
                        .ToArray();

                    evt.menu.AppendAction("Disconnect Nodes", _ =>
                    {
                        CommandDispatcher.Dispatch(new DisconnectNodeCommand(connectedNodes));
                    }, connectedNodes.Length == 0 ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);

                    var ioConnectedNodes = connectedNodes.OfType<IInOutPortsNode>()
                        .Where(x => x.InputsByDisplayOrder.Any(y => y.IsConnected()) &&
                            x.OutputsByDisplayOrder.Any(y => y.IsConnected())).ToArray();

                    evt.menu.AppendAction("Bypass Nodes", _ =>
                    {
                        CommandDispatcher.Dispatch(new BypassNodesCommand(ioConnectedNodes, nodes));
                    }, ioConnectedNodes.Length == 0 ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);

                    var willDisable = nodes.Any(n => n.State == ModelState.Enabled);
                    evt.menu.AppendAction(willDisable ? "Disable Nodes" : "Enable Nodes", _ =>
                    {
                        CommandDispatcher.Dispatch(new SetNodeEnabledStateCommand(nodes, willDisable ? ModelState.Disabled : ModelState.Enabled));
                    });
                }

                var graphElementModels = Selection.OfType<GraphElement>().Select(e => e.Model).ToList();
                if (graphElementModels.Count == 2)
                {
                    // PF: FIXME check conditions correctly for this actions (exclude single port nodes, check if already connected).
                    if (graphElementModels.FirstOrDefault(x => x is IEdgeModel) is IEdgeModel edgeModel &&
                        graphElementModels.FirstOrDefault(x => x is IInOutPortsNode) is IInOutPortsNode nodeModel)
                    {
                        evt.menu.AppendAction("Insert Node on Edge", _ => CommandDispatcher.Dispatch(new SplitEdgeAndInsertExistingNodeCommand(edgeModel, nodeModel)),
                            eventBase => DropdownMenuAction.Status.Normal);
                    }
                }

                var variableNodes = nodes.OfType<IVariableNodeModel>().ToArray();
                if (variableNodes.Length > 0)
                {
                    // TODO JOCE We might want to bring the concept of Get/Set variable from VS down to GTF
                    evt.menu.AppendAction("Variable/Convert",
                        _ => CommandDispatcher.Dispatch(new ConvertVariableNodesToConstantNodesCommand(variableNodes)),
                        variableNodes.Any(v => v.OutputsByDisplayOrder.Any(o => o.PortType == PortType.Data)) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

                    evt.menu.AppendAction("Variable/Itemize",
                        _ => CommandDispatcher.Dispatch(new ItemizeNodeCommand(variableNodes.OfType<ISingleOutputPortNode>().ToArray())),
                        variableNodes.Any(v => v.OutputsByDisplayOrder.Any(o => o.PortType == PortType.Data && o.GetConnectedPorts().Count() > 1)) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                }

                var constants = nodes.OfType<IConstantNodeModel>().ToArray();
                if (constants.Length > 0)
                {
                    evt.menu.AppendAction("Constant/Convert",
                        _ => CommandDispatcher.Dispatch(new ConvertConstantNodesToVariableNodesCommand(constants)), x => DropdownMenuAction.Status.Normal);

                    evt.menu.AppendAction("Constant/Itemize",
                        _ => CommandDispatcher.Dispatch(new ItemizeNodeCommand(constants.OfType<ISingleOutputPortNode>().ToArray())),
                        constants.Any(v => v.OutputsByDisplayOrder.Any(o => o.PortType == PortType.Data && o.GetConnectedPorts().Count() > 1)) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

                    evt.menu.AppendAction("Constant/Lock",
                        _ => CommandDispatcher.Dispatch(new ToggleLockConstantNodeCommand(constants)), x => DropdownMenuAction.Status.Normal);
                }

                var portals = nodes.OfType<IEdgePortalModel>().ToArray();
                if (portals.Length > 0)
                {
                    var canCreate = portals.Where(p => p.CanCreateOppositePortal()).ToArray();
                    evt.menu.AppendAction("Create Opposite Portal",
                        _ =>
                        {
                            CommandDispatcher.Dispatch(new CreateOppositePortalCommand(canCreate));
                        }, canCreate.Length > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                }

                var placemats = Selection.OfType<Placemat>()
                    .Select(e => e.PlacematModel).ToArray();

                if (nodes.Length > 0 || placemats.Length > 0)
                {
                    evt.menu.AppendAction("Color/Change...", _ =>
                    {
                        void ChangeNodesColor(Color pickedColor)
                        {
                            CommandDispatcher.Dispatch(new ChangeElementColorCommand(pickedColor, nodes, placemats));
                        }

                        var defaultColor = new Color(0.5f, 0.5f, 0.5f);
                        if (nodes.Length == 0 && placemats.Length == 1)
                        {
                            defaultColor = placemats[0].Color;
                        }
                        else if (nodes.Length == 1 && placemats.Length == 0)
                        {
                            defaultColor = nodes[0].Color;
                        }

                        GraphViewStaticBridge.ShowColorPicker(ChangeNodesColor, defaultColor, true);
                    });

                    evt.menu.AppendAction("Color/Reset", _ =>
                    {
                        CommandDispatcher.Dispatch(new ResetElementColorCommand(nodes, placemats));
                    });
                }
                else
                {
                    evt.menu.AppendAction("Color", _ => {}, eventBase => DropdownMenuAction.Status.Disabled);
                }

                var edges = Selection.OfType<Edge>().Select(e => e.EdgeModel).ToArray();
                if (edges.Length > 0)
                {
                    evt.menu.AppendSeparator();

                    var edgeData = edges.Select(
                        edgeModel =>
                        {
                            var outputPort = edgeModel.FromPort.GetUI<Port>(this);
                            var inputPort = edgeModel.ToPort.GetUI<Port>(this);
                            var outputNode = edgeModel.FromPort.NodeModel.GetUI<Node>(this);
                            var inputNode = edgeModel.ToPort.NodeModel.GetUI<Node>(this);

                            if (outputNode == null || inputNode == null || outputPort == null || inputPort == null)
                                return (null, Vector2.zero, Vector2.zero);

                            return (edgeModel,
                                outputPort.ChangeCoordinatesTo(outputNode.parent, outputPort.layout.center),
                                inputPort.ChangeCoordinatesTo(inputNode.parent, inputPort.layout.center));
                        }
                        ).Where(tuple => tuple.Item1 != null).ToList();

                    evt.menu.AppendAction("Create Portals", _ =>
                    {
                        CommandDispatcher.Dispatch(new ConvertEdgesToPortalsCommand(edgeData));
                    });
                }

                var stickyNotes = Selection?.OfType<StickyNote>().Select(e => e.StickyNoteModel).ToArray();

                if (stickyNotes.Length > 0)
                {
                    evt.menu.AppendSeparator();

                    DropdownMenuAction.Status GetThemeStatus(DropdownMenuAction a)
                    {
                        if (stickyNotes.Any(noteModel => noteModel.Theme != stickyNotes.First().Theme))
                        {
                            // Values are not all the same.
                            return DropdownMenuAction.Status.Normal;
                        }

                        return stickyNotes.First().Theme == (a.userData as string) ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;
                    }

                    DropdownMenuAction.Status GetSizeStatus(DropdownMenuAction a)
                    {
                        if (stickyNotes.Any(noteModel => noteModel.TextSize != stickyNotes.First().TextSize))
                        {
                            // Values are not all the same.
                            return DropdownMenuAction.Status.Normal;
                        }

                        return stickyNotes.First().TextSize == (a.userData as string) ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;
                    }

                    foreach (var value in StickyNote.GetThemes())
                    {
                        evt.menu.AppendAction("Sticky Note Theme/" + value,
                            menuAction => CommandDispatcher.Dispatch(new UpdateStickyNoteThemeCommand(stickyNotes, menuAction.userData as string)),
                            GetThemeStatus, value);
                    }

                    foreach (var value in StickyNote.GetSizes())
                    {
                        evt.menu.AppendAction("Sticky Note Text Size/" + value,
                            menuAction => CommandDispatcher.Dispatch(new UpdateStickyNoteTextSizeCommand(stickyNotes, menuAction.userData as string)),
                            GetSizeStatus, value);
                    }
                }
            }

            evt.menu.AppendSeparator();

            var models = Selection.OfType<GraphElement>().Select(e => e.Model).ToArray();

            // PF: FIXME use a Command.
            evt.menu.AppendAction("Cut", (a) => { CutSelectionCallback(); },
                CanCutSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendAction("Copy", (a) => { CopySelectionCallback(); },
                CanCopySelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            // PF: FIXME use a Command.
            evt.menu.AppendAction("Paste", (a) => { PasteCallback(); },
                CanPaste ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendSeparator();

            // PF: FIXME use a Command.
            evt.menu.AppendAction("Duplicate", (a) => { DuplicateSelectionCallback(); },
                CanDuplicateSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendAction("Delete", _ =>
            {
                CommandDispatcher.Dispatch(new DeleteElementsCommand(models));
            }, CanDeleteSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            if (Unsupported.IsDeveloperBuild())
            {
                evt.menu.AppendSeparator();
                evt.menu.AppendAction("Internal/Refresh All UI", _ => CommandDispatcher.MarkStateDirty());

                if (Selection.Any())
                {
                    var selectedModels = Selection.OfType<GraphElement>().Select(e => e.Model).ToArray();

                    evt.menu.AppendAction("Internal/Refresh Selected Element(s)",
                        _ =>
                        {
                            CommandDispatcher.GraphToolState.MarkChanged(selectedModels);
                        });
                }
            }
        }

        public virtual void DisplaySmartSearch(Vector2 mousePosition)
        {
            var graphPosition = contentViewContainer.WorldToLocal(mousePosition);
            var element = panel.Pick(mousePosition).GetFirstOfType<IModelUI>();
            switch (element)
            {
                case Edge edge:
                    SearcherService.ShowEdgeNodes(CommandDispatcher.GraphToolState, edge.EdgeModel, mousePosition, item =>
                    {
                        CommandDispatcher.Dispatch(new CreateNodeOnEdgeCommand(edge.EdgeModel, graphPosition, item));
                    });
                    break;

                default:
                    SearcherService.ShowGraphNodes(CommandDispatcher.GraphToolState, mousePosition, item =>
                    {
                        CommandDispatcher.Dispatch(new CreateNodeFromSearcherCommand(graphPosition, item));
                    });
                    break;
            }
        }

        protected override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            if (evt.eventTypeId == DetachFromPanelEvent.TypeId())
            {
                DetachFromPanelEvent dtpe = (DetachFromPanelEvent)evt;

                if (dtpe.destinationPanel == null)
                {
                    Undo.ClearUndo(m_GraphViewUndoRedoSelection);
                    // ReSharper disable once DelegateSubtraction
                    Undo.undoRedoPerformed -= UndoRedoPerformed;
                    Object.DestroyImmediate(m_GraphViewUndoRedoSelection);
                    m_GraphViewUndoRedoSelection = null;

                    if (!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying)
                        ClearSavedSelection();
                }
            }
            else if (evt.eventTypeId == AttachToPanelEvent.TypeId())
            {
                AttachToPanelEvent atpe = (AttachToPanelEvent)evt;

                if (atpe.originPanel == null)
                {
                    Undo.undoRedoPerformed += UndoRedoPerformed;
                    m_GraphViewUndoRedoSelection = ScriptableObject.CreateInstance<GraphViewUndoRedoSelection>();
                    m_GraphViewUndoRedoSelection.hideFlags = HideFlags.HideAndDontSave;
                }
            }
        }

        protected void OnEnterPanel(AttachToPanelEvent e)
        {
            base.OnEnterPanel();
            ShortcutHandler = new ShortcutHandler(GetShortcutDictionary());
        }

        protected void OnLeavePanel(DetachFromPanelEvent e)
        {
            ShortcutHandler = null;
            base.OnLeavePanel();
        }

        bool IsElementVisibleInViewport(VisualElement element)
        {
            return element != null && element.hierarchy.parent.ChangeCoordinatesTo(this, element.layout).Overlaps(layout);
        }

        void FrameSelectionIfNotVisible()
        {
            if (Selection.Cast<VisualElement>().Any(e => !IsElementVisibleInViewport(e)))
                FrameSelection();
        }

        protected void OnValidateCommand(ValidateCommandEvent evt)
        {
            if (panel.GetCapturingElement(PointerId.mousePointerId) != null)
                return;

            if ((evt.commandName == EventCommandNames.Copy && CanCopySelection)
                || (evt.commandName == EventCommandNames.Paste && CanPaste)
                || (evt.commandName == EventCommandNames.Duplicate && CanDuplicateSelection)
                || (evt.commandName == EventCommandNames.Cut && CanCutSelection)
                || ((evt.commandName == EventCommandNames.Delete || evt.commandName == EventCommandNames.SoftDelete) && CanDeleteSelection))
            {
                evt.StopPropagation();
                evt.imguiEvent?.Use();
            }
            else if (evt.commandName == EventCommandNames.FrameSelected)
            {
                evt.StopPropagation();
                evt.imguiEvent?.Use();
            }
        }

        protected void OnExecuteCommand(ExecuteCommandEvent evt)
        {
            if (panel.GetCapturingElement(PointerId.mousePointerId) != null)
                return;

            if (evt.commandName == EventCommandNames.Copy)
            {
                CopySelectionCallback();
                evt.StopPropagation();
            }
            else if (evt.commandName == EventCommandNames.Paste)
            {
                PasteCallback();
                evt.StopPropagation();
            }
            else if (evt.commandName == EventCommandNames.Duplicate)
            {
                DuplicateSelectionCallback();
                evt.StopPropagation();
            }
            else if (evt.commandName == EventCommandNames.Cut)
            {
                CutSelectionCallback();
                evt.StopPropagation();
            }
            else if (evt.commandName == EventCommandNames.Delete)
            {
                DeleteSelection();
                evt.StopPropagation();
            }
            else if (evt.commandName == EventCommandNames.SoftDelete)
            {
                DeleteSelection();
                evt.StopPropagation();
            }
            else if (evt.commandName == EventCommandNames.FrameSelected)
            {
                FrameSelection();
                evt.StopPropagation();
            }

            if (evt.isPropagationStopped)
            {
                evt.imguiEvent?.Use();
            }
        }

        static void CollectElements(IEnumerable<GraphElement> elements, HashSet<GraphElement> collectedElementSet, Func<GraphElement, bool> conditionFunc)
        {
            foreach (var element in elements.Where(e => e != null && !collectedElementSet.Contains(e) && conditionFunc(e)))
            {
                collectedElementSet.Add(element);
            }
        }

        protected virtual void CollectCopyableGraphElements(IEnumerable<GraphElement> elements, HashSet<GraphElement> elementsToCopySet)
        {
            var elementList = elements.ToList();
            CollectElements(elementList, elementsToCopySet, e => e.IsCopiable());

            // Also collect hovering list of nodes
            foreach (var placemat in elementList.OfType<Placemat>())
            {
                placemat.ActOnGraphElementsOver(
                    el =>
                    {
                        CollectElements(new[] { el },
                            elementsToCopySet,
                            e => e.IsCopiable());
                        return false;
                    },
                    true);
            }
        }

        protected void CopySelectionCallback()
        {
            var elementsToCopySet = new HashSet<GraphElement>();

            CollectCopyableGraphElements(Selection.OfType<GraphElement>(), elementsToCopySet);

            string data = SerializeGraphElements(elementsToCopySet);

            if (!string.IsNullOrEmpty(data))
            {
                Clipboard = data;
            }
        }

        protected void CutSelectionCallback()
        {
            CopySelectionCallback();
            DeleteSelection("Cut");
        }

        protected void PasteCallback()
        {
            UnserializeAndPasteOperation("Paste", Clipboard);
        }

        protected void DuplicateSelectionCallback()
        {
            var elementsToCopySet = new HashSet<GraphElement>();

            CollectCopyableGraphElements(Selection.OfType<GraphElement>(), elementsToCopySet);

            string serializedData = SerializeGraphElements(elementsToCopySet);

            UnserializeAndPasteOperation("Duplicate", serializedData);
        }

        protected string SerializeGraphElements(IEnumerable<GraphElement> elements)
        {
            if (SerializeGraphElementsCallback != null)
            {
                string data = SerializeGraphElementsCallback(elements);
                if (!string.IsNullOrEmpty(data))
                {
                    data = k_SerializedDataMimeType + " " + data;
                }
                return data;
            }
            else
            {
                return string.Empty;
            }
        }

        protected bool CanPasteSerializedData(string data)
        {
            if (CanPasteSerializedDataCallback != null)
            {
                if (data.StartsWith(k_SerializedDataMimeType))
                {
                    return CanPasteSerializedDataCallback(data.Substring(k_SerializedDataMimeType.Length + 1));
                }
                else
                {
                    return CanPasteSerializedDataCallback(data);
                }
            }
            if (data.StartsWith(k_SerializedDataMimeType))
            {
                return true;
            }
            return false;
        }

        protected void UnserializeAndPasteOperation(string operationName, string data)
        {
            if (UnserializeAndPasteCallback != null)
            {
                if (data.StartsWith(k_SerializedDataMimeType))
                {
                    UnserializeAndPasteCallback(operationName, data.Substring(k_SerializedDataMimeType.Length + 1));
                }
                else
                {
                    UnserializeAndPasteCallback(operationName, data);
                }
            }
        }

        public virtual void AddElement(GraphElement graphElement)
        {
            if (graphElement is Badge)
            {
                m_BadgesParent.Add(graphElement);
            }
            else if (!(graphElement is Placemat))
            {
                // Placemats come in already added to the right spot.

                int newLayer = graphElement.Layer;
                if (!m_ContainerLayers.ContainsKey(newLayer))
                {
                    AddLayer(newLayer);
                }

                GetLayer(newLayer).Add(graphElement);
            }

            try
            {
                // Attempt to restore selection on the new element if it
                // was previously selected (same GUID).
                RestorePersistentSelectionForElement(graphElement);

                graphElement.AddToGraphView(this);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            if (graphElement.Model != null && CommandDispatcher?.GraphToolState != null &&
                CommandDispatcher.GraphToolState.SelectionStateComponent.ShouldSelectElementUponCreation(graphElement.Model))
            {
                graphElement.Select(this, true);
            }

            if (graphElement is Node || graphElement is Edge)
                graphElement.RegisterCallback<MouseOverEvent>(OnMouseOver);

            if (graphElement.Model is IEdgePortalModel portalModel)
            {
                AddPortalDependency(portalModel);
            }
        }

        public virtual void RemoveElement(GraphElement graphElement, bool unselectBeforeRemove = false)
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

            if (unselectBeforeRemove)
            {
                graphElement.Unselect(this);
            }

            if (graphElement is Placemat placemat)
            {
                PlacematContainer.RemovePlacemat(placemat);
            }
            else
            {
                graphElement.RemoveFromHierarchy();
            }

            graphElement.RemoveFromGraphView();
        }

        public void DeleteSelection(string operationName = "Delete")
        {
            IGraphElementModel[] elementsToRemove = Selection.Cast<GraphElement>()
                .Select(x => x.Model)
                .Where(m => m != null).ToArray(); // 'this' has no model
            CommandDispatcher.Dispatch(new DeleteElementsCommand(elementsToRemove) { UndoString = operationName });
        }

        public void FrameAll()
        {
            Frame(FrameType.All);
        }

        public void FrameSelection()
        {
            Frame(FrameType.Selection);
        }

        public void FrameOrigin()
        {
            Frame(FrameType.Origin);
        }

        public void FramePrev()
        {
            List<GraphElement> childrenList = GraphElements.ToList().Where(e => e.IsSelectable() && !(e is Edge)).OrderByDescending(e => e.controlid).ToList();
            FramePrevNext(childrenList);
        }

        public void FrameNext()
        {
            List<GraphElement> childrenList = GraphElements.ToList().Where(e => e.IsSelectable() && !(e is Edge)).OrderBy(e => e.controlid).ToList();
            FramePrevNext(childrenList);
        }

        public void FramePrev(Func<GraphElement, bool> predicate)
        {
            if (contentViewContainer.childCount == 0) return;
            List<GraphElement> list = GraphElements.ToList().Where(predicate).ToList();
            list.Reverse();
            FramePrevNext(list);
        }

        public void FrameNext(Func<GraphElement, bool> predicate)
        {
            if (contentViewContainer.childCount == 0) return;
            FramePrevNext(GraphElements.ToList().Where(predicate).ToList());
        }

        void FramePrevNext(List<GraphElement> childrenList)
        {
            if (childrenList.Count == 0)
                return;

            GraphElement graphElement = null;

            // Start from current selection, if any
            if (Selection.Count != 0)
                graphElement = Selection[0] as GraphElement;

            int idx = childrenList.IndexOf(graphElement);

            if (idx >= 0 && idx < childrenList.Count - 1)
                graphElement = childrenList[idx + 1];
            else
                graphElement = childrenList[0];

            // New selection...
            ClearSelection();
            AddToSelection(graphElement);

            // ...and frame this new selection
            Frame(FrameType.Selection);
        }

        void Frame(FrameType frameType)
        {
            Rect rectToFit = contentViewContainer.layout;
            Vector3 frameTranslation = Vector3.zero;
            Vector3 frameScaling = Vector3.one;

            if (frameType == FrameType.Selection &&
                (Selection.Count == 0 || !Selection.Any(e => e.IsSelectable() && !(e is Edge))))
                frameType = FrameType.All;

            if (frameType == FrameType.Selection)
            {
                VisualElement graphElement = Selection[0] as GraphElement;
                if (graphElement != null)
                {
                    // Edges don't have a size. Only their internal EdgeControl have a size.
                    if (graphElement is Edge)
                        graphElement = (graphElement as Edge).EdgeControl;
                    rectToFit = graphElement.ChangeCoordinatesTo(contentViewContainer, graphElement.GetRect());
                }

                rectToFit = Selection.Cast<GraphElement>()
                    .Aggregate(rectToFit, (current, currentGraphElement) =>
                    {
                        VisualElement currentElement = currentGraphElement;
                        if (currentGraphElement is Edge)
                            currentElement = (currentGraphElement as Edge).EdgeControl;
                        return RectUtils.Encompass(current, currentElement.ChangeCoordinatesTo(contentViewContainer, currentElement.GetRect()));
                    });
                CalculateFrameTransform(rectToFit, layout, k_FrameBorder, out frameTranslation, out frameScaling);
            }
            else if (frameType == FrameType.All)
            {
                rectToFit = CalculateRectToFitAll(contentViewContainer);
                CalculateFrameTransform(rectToFit, layout, k_FrameBorder, out frameTranslation, out frameScaling);
            } // else keep going if (frameType == FrameType.Origin)

            Matrix4x4.TRS(frameTranslation, Quaternion.identity, frameScaling);
            UpdateViewTransform(frameTranslation, frameScaling);

            contentViewContainer.MarkDirtyRepaint();

            UpdatePersistedViewTransform();
        }

        public virtual Rect CalculateRectToFitAll(VisualElement container)
        {
            Rect rectToFit = container.layout;
            bool reachedFirstChild = false;

            GraphElements.ForEach(ge =>
            {
                if (ge.Model is IEdgeModel)
                {
                    return;
                }

                if (!reachedFirstChild)
                {
                    rectToFit = ge.ChangeCoordinatesTo(contentViewContainer, ge.GetRect());
                    reachedFirstChild = true;
                }
                else
                {
                    rectToFit = RectUtils.Encompass(rectToFit, ge.ChangeCoordinatesTo(contentViewContainer, ge.GetRect()));
                }
            });

            return rectToFit;
        }

        public void CalculateFrameTransform(Rect rectToFit, Rect clientRect, int border, out Vector3 frameTranslation, out Vector3 frameScaling)
        {
            // bring slightly smaller screen rect into GUI space
            var screenRect = new Rect
            {
                xMin = border,
                xMax = clientRect.width - border,
                yMin = border,
                yMax = clientRect.height - border
            };

            Matrix4x4 m = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
            Rect identity = GUIUtility.ScreenToGUIRect(screenRect);

            // measure zoom level necessary to fit the canvas rect into the screen rect
            float zoomLevel = Math.Min(identity.width / rectToFit.width, identity.height / rectToFit.height);

            // clamp
            zoomLevel = Mathf.Clamp(zoomLevel, m_MinScale, Math.Min(m_MaxScale, m_MaxScaleOnFrame));

            var trs = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(zoomLevel, zoomLevel, 1.0f));

            var edge = new Vector2(clientRect.width, clientRect.height);
            var origin = new Vector2(0, 0);

            var r = new Rect
            {
                min = origin,
                max = edge
            };

            var parentScale = new Vector3(trs.GetColumn(0).magnitude,
                trs.GetColumn(1).magnitude,
                trs.GetColumn(2).magnitude);
            Vector2 offset = r.center - (rectToFit.center * parentScale.x);

            // Update output values before leaving
            frameTranslation = new Vector3(offset.x, offset.y, 0.0f);
            frameScaling = parentScale;

            GUI.matrix = m;
        }

        /// <summary>
        /// Pan the graph view to get the node referred in parameter in the center of the display.
        /// </summary>
        /// <param name="nodeGuid">The GUID of the node to pan to.</param>
        public void PanToNode(SerializableGUID nodeGuid)
        {
            var graphModel = CommandDispatcher.GraphToolState.GraphModel;

            if (!graphModel.NodesByGuid.TryGetValue(nodeGuid, out var nodeModel))
                return;

            var graphElement = nodeModel.GetUI<GraphElement>(this);
            if (graphElement == null)
                return;

            graphElement.Select(this, false);
            FrameSelection();
        }

        protected void AddPositionDependency(IEdgeModel model)
        {
            PositionDependenciesManager.AddPositionDependency(model);
        }

        protected void RemovePositionDependency(IEdgeModel edgeModel)
        {
            PositionDependenciesManager.Remove(edgeModel.FromNodeGuid, edgeModel.ToNodeGuid);
            PositionDependenciesManager.LogDependencies();
        }

        protected void AddPortalDependency(IEdgePortalModel model)
        {
            PositionDependenciesManager.AddPortalDependency(model);
        }

        protected void RemovePortalDependency(IEdgePortalModel model)
        {
            PositionDependenciesManager.RemovePortalDependency(model);
            PositionDependenciesManager.LogDependencies();
        }

        public virtual void StopSelectionDragger()
        {
            // cancellation is handled in the MoveMove callback
            m_SelectionDraggerWasActive = false;
        }

        protected virtual Dictionary<Event, ShortcutDelegate> GetShortcutDictionary()
        {
            if (CommandDispatcher == null)
            {
                return new Dictionary<Event, ShortcutDelegate>();
            }

            var shortcuts = new Dictionary<Event, ShortcutDelegate>
            {
                {
                    Event.KeyboardEvent("F5"), _ =>
                    {
                        CommandDispatcher.MarkStateDirty();
                        return ShortcutHandled.Handled;
                    }
                },
                {
                    Event.KeyboardEvent("backspace"), _ =>
                    {
                        var selectedNodes = Selection.OfType<IModelUI>()
                            .Select(x => x.Model).OfType<IInOutPortsNode>().ToArray();

                        if (!selectedNodes.Any())
                            return ShortcutHandled.NotHandled;

                        var connectedNodes = selectedNodes.Where(x => x.InputsById.Values
                            .Any(y => y.IsConnected()) && x.OutputsById.Values.Any(y => y.IsConnected()))
                            .ToArray();

                        var canSelectionBeBypassed = connectedNodes.Any();
                        if (canSelectionBeBypassed)
                            CommandDispatcher.Dispatch(new BypassNodesCommand(connectedNodes, selectedNodes.ToArray<INodeModel>()));
                        else
                            CommandDispatcher.Dispatch(new DeleteElementsCommand(selectedNodes.Cast<IGraphElementModel>().ToArray()));

                        return ShortcutHandled.Handled;
                    }
                },
                {
                    Event.KeyboardEvent("space"), e =>
                    {
                        DisplaySmartSearch(e.originalMousePosition);
                        return ShortcutHandled.Handled;
                    }
                },
                {
                    Event.KeyboardEvent("C"), _ =>
                    {
                        var selectedModels = Selection
                            .OfType<IModelUI>()
                            .Select(x => x.Model)
                            .ToArray();

                        // Convert variable -> constant if selection contains at least one item that satisfies conditions
                        var variableModels = selectedModels.OfType<IVariableNodeModel>().ToArray();
                        if (variableModels.Any())
                        {
                            CommandDispatcher.Dispatch(new ConvertVariableNodesToConstantNodesCommand(variableModels));
                        }
                        else
                        {
                            var constantModels = selectedModels.OfType<IConstantNodeModel>().ToArray();
                            if (constantModels.Any())
                                CommandDispatcher.Dispatch(new ConvertConstantNodesToVariableNodesCommand(constantModels));
                        }

                        return ShortcutHandled.Handled;
                    }
                },
                {
                    Event.KeyboardEvent("Q"), _ =>
                    {
                        CommandDispatcher.Dispatch(new AlignNodesCommand(this, false));
                        return ShortcutHandled.Handled;
                    }
                },
                {
                    Event.KeyboardEvent("#Q"), _ =>
                    {
                        CommandDispatcher.Dispatch(new AlignNodesCommand(this, true));
                        return ShortcutHandled.Handled;
                    }
                },
                {
                    Event.KeyboardEvent("`"), e =>
                    {
                        var atPosition = new Rect(this.ChangeCoordinatesTo(contentViewContainer, this.WorldToLocal(e.originalMousePosition)), StickyNote.defaultSize);
                        CommandDispatcher.Dispatch(new CreateStickyNoteCommand(atPosition));
                        return ShortcutHandled.Handled;
                    }
                },
                {
                    Event.KeyboardEvent("a"), e =>
                    {
                        if (panel.GetCapturingElement(PointerId.mousePointerId) == null)
                        {
                            FrameAll();
                            return ShortcutHandled.Handled;
                        }

                        return ShortcutHandled.NotHandled;
                    }
                },
                {
                    Event.KeyboardEvent("o"), e =>
                    {
                        if (panel.GetCapturingElement(PointerId.mousePointerId) == null)
                        {
                            FrameOrigin();
                            return ShortcutHandled.Handled;
                        }

                        return ShortcutHandled.NotHandled;
                    }
                },
                {
                    Event.KeyboardEvent("["), e =>
                    {
                        if (panel.GetCapturingElement(PointerId.mousePointerId) == null)
                        {
                            FramePrev();
                            return ShortcutHandled.Handled;
                        }

                        return ShortcutHandled.NotHandled;
                    }
                },
                {
                    Event.KeyboardEvent("]"), e =>
                    {
                        if (panel.GetCapturingElement(PointerId.mousePointerId) == null)
                        {
                            FrameNext();
                            return ShortcutHandled.Handled;
                        }

                        return ShortcutHandled.NotHandled;
                    }
                },
            };

            if (Application.platform != RuntimePlatform.OSXEditor)
            {
                shortcuts.Add(Event.KeyboardEvent("F2"), BeginEditSelection);
            }
            else
            {
                shortcuts.Add(Event.KeyboardEvent("[enter]"), BeginEditSelection);
                shortcuts.Add(Event.KeyboardEvent("return"), BeginEditSelection);
            }

            return shortcuts;
        }

        ShortcutHandled BeginEditSelection(KeyDownEvent e)
        {
            var renamableSelection = Selection.OfType<GraphElement>().Where(x => x.IsRenamable()).ToList();
            var lastSelectedItem = renamableSelection.LastOrDefault();
            if (lastSelectedItem != null)
            {
                if (renamableSelection.Count > 1)
                {
                    ClearSelection();
                    AddToSelection(lastSelectedItem);
                }

                FrameSelectionIfNotVisible();
                lastSelectedItem.Rename();

                return ShortcutHandled.Handled;
            }

            return ShortcutHandled.NotHandled;
        }

        protected void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            if (Window != null)
            {
                // Set the window min size from the graphView
                Window.AdjustWindowMinSize(new Vector2(resolvedStyle.minWidth.value, resolvedStyle.minHeight.value));
            }
        }

        protected void OnMouseOver(MouseOverEvent evt)
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

        protected void OnDoubleClick()
        {
            // Display graph in inspector when clicking on background
            // TODO: displayed on double click ATM as this method overrides the Token.Select() which does not stop propagation
            UnityEditor.Selection.activeObject = CommandDispatcher?.GraphToolState?.AssetModel as Object;
        }

        protected void OnMouseMove(MouseMoveEvent evt)
        {
            if (m_SelectionDraggerWasActive && !SelectionDragger.IsActive) // cancelled
            {
                m_SelectionDraggerWasActive = false;
                PositionDependenciesManager.CancelMove();
            }
            else if (!m_SelectionDraggerWasActive && SelectionDragger.IsActive) // started
            {
                m_SelectionDraggerWasActive = true;

                GraphElement elem = (GraphElement)Selection.FirstOrDefault(x => x is IModelUI hasModel && hasModel.Model is INodeModel);
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
                    if (SelectionDragger.IsActive && moveNodeDependencies) // processed
                    {
                        Vector2 pos = contentViewContainer.ChangeCoordinatesTo(elem.hierarchy.parent, elem.GetPosition().position);
                        PositionDependenciesManager.ProcessMovedNodes(pos);
                    }
                }).Until(() => !m_SelectionDraggerWasActive);
            }

            m_LastMousePosition = this.ChangeCoordinatesTo(contentViewContainer, evt.localMousePosition);
        }

        public virtual void OnDragEnter(DragEnterEvent evt)
        {
            var stencil = CommandDispatcher.GraphToolState.GraphModel.Stencil;
            var e = GetExternalDragNDropHandler(evt);
            if (e != null)
            {
                m_CurrentDragAndDropHandler = e;
                e.OnDragEnter(evt);
            }
        }

        public virtual void OnDragLeave(DragLeaveEvent evt)
        {
            m_CurrentDragAndDropHandler?.OnDragLeave(evt);
            m_CurrentDragAndDropHandler = null;
        }

        public virtual void OnDragUpdated(DragUpdatedEvent e)
        {
            m_CurrentDragAndDropHandler?.OnDragUpdated(e);
            e.StopPropagation();
        }

        public virtual void OnDragPerform(DragPerformEvent e)
        {
            m_CurrentDragAndDropHandler?.OnDragPerform(e);
            m_CurrentDragAndDropHandler = null;
            e.StopPropagation();
        }

        public virtual void OnDragExited(DragExitedEvent e)
        {
            m_CurrentDragAndDropHandler?.OnDragExited(e);
            m_CurrentDragAndDropHandler = null;
        }

        protected static string OnSerializeGraphElements(IEnumerable<GraphElement> elements)
        {
            CopyPasteData.s_LastCopiedData = CopyPasteData.GatherCopiedElementsData(elements
                .Select(x => x.Model)
                .ToList());
            return CopyPasteData.s_LastCopiedData.IsEmpty() ? string.Empty : "data";
        }

        void UnserializeAndPaste(string operationName, string data)
        {
            if (CopyPasteData.s_LastCopiedData == null || CopyPasteData.s_LastCopiedData.IsEmpty())//string.IsNullOrEmpty(data))
                return;


            var delta = m_LastMousePosition - CopyPasteData.s_LastCopiedData.topLeftNodePosition;

            TargetInsertionInfo info;
            info.OperationName = operationName;
            info.Delta = delta;

            CommandDispatcher.Dispatch(new PasteSerializedDataCommand(info, CopyPasteData.s_LastCopiedData));
        }

        public virtual void UpdateUI(UIRebuildType rebuildType)
        {
            if (CommandDispatcher?.GraphToolState == null)
                return;

            if (rebuildType == UIRebuildType.Complete || CommandDispatcher.GraphToolState.NewModels.Any())
            {
                RebuildAll(CommandDispatcher.GraphToolState);

                // PF FIXME: This should not be necessary
                this.HighlightGraphElements();
            }
            else if (rebuildType == UIRebuildType.Partial)
            {
                foreach (var model in CommandDispatcher.GraphToolState.DeletedModels)
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

                foreach (var model in CommandDispatcher.GraphToolState.ChangedModels)
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
            if (CommandDispatcher.GraphToolState.Preferences.GetBool(BoolPref.ShowUnusedNodes))
                PositionDependenciesManager.UpdateNodeState();

            if (CommandDispatcher.GraphToolState.ModelsToAutoAlign.Any())
            {
                // Auto placement relies on UI layout to compute node positions, so we need to
                // schedule it to execute after the next layout pass.
                // Furthermore, it will modify the model position, hence it must be
                // done inside a Store.BeginStateChange block.
                var elementsToAlign = CommandDispatcher.GraphToolState.ModelsToAutoAlign.ToList();
                schedule.Execute(() =>
                {
                    CommandDispatcher.BeginStateChange();
                    PositionDependenciesManager.AlignNodes(true, elementsToAlign);
                    CommandDispatcher.EndStateChange();
                });
            }

            CommandDispatcher.GraphToolState.SelectionStateComponent.ClearElementsToSelectUponCreation();
        }

        void RebuildAll(GraphToolState state)
        {
            ClearGraph();

            var graphModel = state.GraphModel;
            if (graphModel == null)
                return;

            PlacematContainer.RemoveAllPlacemats();

            foreach (var nodeModel in graphModel.NodeModels)
            {
                var node = GraphElementFactory.CreateUI<GraphElement>(this, CommandDispatcher, nodeModel);
                if (node != null)
                    AddElement(node);
            }

            foreach (var stickyNoteModel in graphModel.StickyNoteModels)
            {
                var stickyNote = GraphElementFactory.CreateUI<GraphElement>(this, CommandDispatcher, stickyNoteModel);
                if (stickyNote != null)
                    AddElement(stickyNote);
            }

            int index = 0;
            foreach (var edge in graphModel.EdgeModels)
            {
                if (!CreateEdgeUI(edge))
                {
                    Debug.LogWarning($"Edge {index} cannot be restored: {edge}");
                }
                index++;
            }

            foreach (var placematModel in state.GraphModel.PlacematModels.OrderBy(e => e.ZOrder))
            {
                var placemat = GraphElementFactory.CreateUI<GraphElement>(this, CommandDispatcher, placematModel);
                if (placemat != null)
                    AddElement(placemat);
            }

            contentViewContainer.Add(m_BadgesParent);

            m_BadgesParent.Clear();
            foreach (var badgeModel in graphModel.BadgeModels)
            {
                if (badgeModel.ParentModel == null)
                    continue;

                var badge = GraphElementFactory.CreateUI<Badge>(this, CommandDispatcher, badgeModel);
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

            m_Blackboard?.SetupBuildAndUpdate(state.BlackboardGraphModel, CommandDispatcher, this);
        }

        bool CreateEdgeUI(IEdgeModel edge)
        {
            if (edge.ToPort != null && edge.FromPort != null)
            {
                AddEdgeUI(edge);
                return true;
            }

            if (edge is IEdgeModel e)
            {
                var(inputResult, outputResult) = e.TryMigratePorts(out var inputNode, out var outputNode);

                if (inputResult == PortMigrationResult.PlaceholderPortAdded && inputNode != null)
                {
                    var inputNodeUi = inputNode.GetUI(this);
                    inputNodeUi?.UpdateFromModel();
                }

                if (outputResult == PortMigrationResult.PlaceholderPortAdded && outputNode != null)
                {
                    var outputNodeUi = outputNode.GetUI(this);
                    outputNodeUi?.UpdateFromModel();
                }

                if (inputResult != PortMigrationResult.PlaceholderPortFailure &&
                    outputResult != PortMigrationResult.PlaceholderPortFailure)
                {
                    AddEdgeUI(edge);
                    return true;
                }
            }

            return false;
        }

        void AddEdgeUI(IEdgeModel edgeModel)
        {
            var edge = GraphElementFactory.CreateUI<GraphElement>(this, CommandDispatcher, edgeModel);
            AddElement(edge);
            AddPositionDependency(edgeModel);
        }
    }
}
