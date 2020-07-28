using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    interface IGraphViewSelection
    {
        int Version { get; set; }

        HashSet<string> SelectedElements { get; }
    }

    class GraphViewUndoRedoSelection : ScriptableObject, IGraphViewSelection, ISerializationCallbackReceiver
    {
        [SerializeField]
        int m_Version;

        [SerializeField]
        string[] m_SelectedElementsArray;

        [NonSerialized]
        HashSet<string> m_SelectedElements = new HashSet<string>();

        public int Version
        {
            get => m_Version;
            set => m_Version = value;
        }

        public HashSet<string> SelectedElements => m_SelectedElements;

        public void OnBeforeSerialize()
        {
            if (m_SelectedElements.Count == 0)
                return;

            m_SelectedElementsArray = new string[m_SelectedElements.Count];

            m_SelectedElements.CopyTo(m_SelectedElementsArray);
        }

        public void OnAfterDeserialize()
        {
            m_SelectedElements.Clear();

            if (m_SelectedElementsArray == null || m_SelectedElementsArray.Length == 0)
                return;

            foreach (string guid in m_SelectedElementsArray)
            {
                m_SelectedElements.Add(guid);
            }
        }
    }

    public struct GraphViewChange
    {
        // Operations Pending
        public List<GraphElement> elementsToRemove;

        // Operations Completed
        public IEnumerable<GraphElement> movedElements;
        public Vector2 moveDelta;
    }

    public abstract class GraphView : GraphViewBridge, ISelection
    {
        // Layer class. Used for queries below.
        public class Layer : VisualElement {}

        public delegate GraphViewChange GraphViewChanged(GraphViewChange graphViewChange);
        public delegate void ElementResized(VisualElement visualElement);
        public delegate void ViewTransformChanged(GraphView graphView);
        public delegate string SerializeGraphElementsDelegate(IEnumerable<GraphElement> elements);
        public delegate bool CanPasteSerializedDataDelegate(string data);
        public delegate void UnserializeAndPasteDelegate(string operationName, string data);

        [Serializable]
        [MovedFrom(false, "Unity.GraphElements", "Unity.GraphTools.Foundation.Overdrive.Editor")]
        class PersistedSelection : IGraphViewSelection, ISerializationCallbackReceiver
        {
            [SerializeField]
            int m_Version;

            [SerializeField]
            string[] m_SelectedElementsArray;

            [NonSerialized]
            HashSet<string> m_SelectedElements = new HashSet<string>();

            public int Version
            {
                get => m_Version;
                set => m_Version = value;
            }

            public HashSet<string> SelectedElements => m_SelectedElements;

            public void OnBeforeSerialize()
            {
                if (m_SelectedElements.Count == 0)
                    return;

                m_SelectedElementsArray = new string[m_SelectedElements.Count];

                m_SelectedElements.CopyTo(m_SelectedElementsArray);
            }

            public void OnAfterDeserialize()
            {
                if (m_SelectedElementsArray == null || m_SelectedElementsArray.Length == 0)
                    return;

                m_SelectedElements.Clear();

                foreach (string guid in m_SelectedElementsArray)
                {
                    m_SelectedElements.Add(guid);
                }
            }
        }

        [Serializable]
        [MovedFrom(false, "Unity.GraphElements", "Unity.GraphTools.Foundation.Overdrive.Editor")]
        class PersistedViewTransform
        {
            public Vector3 position = Vector3.zero;
            public Vector3 scale = Vector3.one;
        }

        class ContentViewContainer : VisualElement
        {
            public override bool Overlaps(Rect r)
            {
                return true;
            }
        }

        enum FrameType
        {
            All = 0,
            Selection = 1,
            Origin = 2
        }

        static readonly string k_SelectionUndoRedoLabel = "Change GraphView Selection";

        static readonly int k_FrameBorder = 30;

        const string k_SerializedDataMimeType = "application/vnd.unity.graphview.elements";

        Store m_Store;

        GraphViewChange m_GraphViewChange;

        List<GraphElement> m_ElementsToRemove;

        int m_SavedSelectionVersion;

        PersistedSelection m_PersistedSelection;

        GraphViewUndoRedoSelection m_GraphViewUndoRedoSelection;

        bool m_FrameAnimate = false;

        readonly Dictionary<int, Layer> m_ContainerLayers = new Dictionary<int, Layer>();

        PlacematContainer m_PlacematContainer;

        IVisualElementScheduledItem m_OnTimerTicker;

        PersistedViewTransform m_PersistedViewTransform;

        UQueryState<GraphElement> m_AllGraphElements;

        ContentZoomer m_Zoomer;
        float m_MinScale = ContentZoomer.DefaultMinScale;
        float m_MaxScale = ContentZoomer.DefaultMaxScale;
        float m_ScaleStep = ContentZoomer.DefaultScaleStep;
        float m_ReferenceScale = ContentZoomer.DefaultReferenceScale;

        float MinScale => m_MinScale;

        float MaxScale => m_MaxScale;

        string m_Clipboard = string.Empty;

        public Store Store => m_Store;

        public UQueryState<GraphElement> GraphElements { get; }

        public UQueryState<Node> Nodes { get; }

        public UQueryState<Port> Ports;

        public UQueryState<Edge> Edges { get; }

        public UQueryState<StickyNote> Stickies { get; }

        public GraphViewChanged GraphViewChangedCallback { get; set; }

        public ElementResized ElementResizedCallback { get; set; }

        public ViewTransformChanged ViewTransformChangedCallback { get; set; }

        public virtual bool SupportsWindowedBlackboard => false;

        public bool PersistedSelectionRestoreEnabled { get; private set; }

        public override VisualElement contentContainer => GraphViewContainer; // Contains full content, potentially partially visible

        public PlacematContainer PlacematContainer
        {
            get
            {
                if (m_PlacematContainer == null)
                {
                    m_PlacematContainer = CreatePlacematContainer();
                    AddLayer(m_PlacematContainer, PlacematContainer.PlacematsLayer);
                }

                return m_PlacematContainer;
            }
        }

        public List<ISelectableGraphElement> Selection { get; }

        public virtual bool CanCopySelection => Selection.Cast<GraphElement>().Any(ge => ge.IsCopiable());

        public virtual bool CanCutSelection => Selection.Any(s => s is Node || s is Placemat);

        public virtual bool CanPaste => CanPasteSerializedData(Clipboard);

        public virtual bool CanDuplicateSelection => CanCopySelection;

        public virtual bool CanDeleteSelection
        {
            get
            {
                return Selection.Cast<GraphElement>().Any(e => e != null && e.IsDeletable());
            }
        }

        public SerializeGraphElementsDelegate SerializeGraphElementsCallback { get; set; }

        public CanPasteSerializedDataDelegate CanPasteSerializedDataCallback { get; set; }

        public UnserializeAndPasteDelegate UnserializeAndPasteCallback { get; set; }

        public virtual IEnumerable<IHighlightable> Highlightables => GraphElements.ToList().OfType<IHighlightable>();

        // The system clipboard is unreliable, at least on Windows.
        // For testing clipboard operations on GraphView,
        // set useInternalClipboard to true.
        internal bool UseInternalClipboard { get; set; }

        internal string Clipboard
        {
            get
            {
                if (UseInternalClipboard)
                {
                    return m_Clipboard;
                }
                else
                {
                    return EditorGUIUtility.systemCopyBuffer;
                }
            }

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

        VisualElement GraphViewContainer { get; }

        bool IsReframable { get; }

        protected GraphView(Store store)
        {
            m_Store = store;

            PersistedSelectionRestoreEnabled = true;

            AddToClassList("graphView");

            this.SetRenderHintsForGraphView();

            Selection = new List<ISelectableGraphElement>();
            style.overflow = Overflow.Hidden;

            style.flexDirection = FlexDirection.Column;

            GraphViewContainer = new VisualElement();
            GraphViewContainer.style.flexGrow = 1f;
            GraphViewContainer.style.flexBasis = 0f;
            GraphViewContainer.pickingMode = PickingMode.Ignore;
            hierarchy.Add(GraphViewContainer);

            contentViewContainer = new ContentViewContainer
            {
                name = "contentViewContainer",
                pickingMode = PickingMode.Ignore,
                usageHints = UsageHints.GroupTransform
            };

            // make it absolute and 0 sized so it acts as a transform to move children to and fro
            GraphViewContainer.Add(contentViewContainer);

            this.AddStylesheet("GraphView.uss");
            GraphElements = contentViewContainer.Query<GraphElement>().Build();
            m_AllGraphElements = this.Query<GraphElement>().Build();
            Nodes = contentViewContainer.Query<Node>().Build();
            Edges = this.Query<Layer>().Children<Edge>().Build();
            Stickies = this.Query<Layer>().Children<StickyNote>().Build();
            Ports = contentViewContainer.Query().Children<Layer>().Descendents<Port>().Build();

            m_ElementsToRemove = new List<GraphElement>();
            m_GraphViewChange.elementsToRemove = m_ElementsToRemove;

            IsReframable = true;
            focusable = true;

            RegisterCallback<ValidateCommandEvent>(OnValidateCommand);
            RegisterCallback<ExecuteCommandEvent>(OnExecuteCommand);
            RegisterCallback<AttachToPanelEvent>(OnEnterPanel);
            RegisterCallback<DetachFromPanelEvent>(OnLeavePanel);
            RegisterCallback<ContextualMenuPopulateEvent>(OnContextualMenu);
        }

        public bool PersistentSelectionContainsElement(GraphElement element)
        {
            if (string.IsNullOrEmpty(element.viewDataKey))
                return false;

            return m_PersistedSelection?.SelectedElements?.Contains(element.viewDataKey) ?? false;
        }

        public void EnablePersistedSelectionRestore()
        {
            PersistedSelectionRestoreEnabled = true;
        }

        public void DisablePersistedSelectionRestore()
        {
            PersistedSelectionRestoreEnabled = false;
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

        internal void RestorePersitentSelectionForElement(GraphElement element)
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

        public void SetupZoom(float minScaleSetup, float maxScaleSetup)
        {
            SetupZoom(minScaleSetup, maxScaleSetup, m_ScaleStep, m_ReferenceScale);
        }

        public void SetupZoom(float minScaleSetup, float maxScaleSetup, float scaleStepSetup, float referenceScaleSetup)
        {
            m_MinScale = minScaleSetup;
            m_MaxScale = maxScaleSetup;
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
        }

        void UpdateContentZoomer()
        {
            if (m_MinScale != m_MaxScale)
            {
                if (m_Zoomer == null)
                {
                    m_Zoomer = new ContentZoomer();
                    this.AddManipulator(m_Zoomer);
                }

                m_Zoomer.minScale = m_MinScale;
                m_Zoomer.maxScale = m_MaxScale;
                m_Zoomer.scaleStep = m_ScaleStep;
                m_Zoomer.referenceScale = m_ReferenceScale;
            }
            else
            {
                if (m_Zoomer != null)
                    this.RemoveManipulator(m_Zoomer);
            }

            ValidateTransform();
        }

        void ValidateTransform()
        {
            if (contentViewContainer == null)
                return;
            Vector3 transformScale = viewTransform.scale;

            transformScale.x = Mathf.Clamp(transformScale.x, MinScale, MaxScale);
            transformScale.y = Mathf.Clamp(transformScale.y, MinScale, MaxScale);

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
        }

        void AddToSelectionNoUndoRecord(GraphElement graphElement)
        {
            graphElement.Selected = true;
            Selection.Add(graphElement);
            graphElement.OnSelected();

            // To ensure that the selected GraphElement gets unselected if it is removed from the GraphView.
            graphElement.RegisterCallback<DetachFromPanelEvent>(OnSelectedElementDetachedFromPanel);

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

            return selectionWasNotEmpty;
        }

        public virtual void ClearSelection()
        {
            if (PersistedSelectionRestoreEnabled)
            {
                bool selectionWasNotEmpty = ClearSelectionNoUndoRecord();

                if (ShouldRecordUndo() && selectionWasNotEmpty)
                {
                    RecordSelectionUndoPre();
                    m_GraphViewUndoRedoSelection.SelectedElements.Clear();
                    m_PersistedSelection.SelectedElements.Clear();
                    RecordSelectionUndoPost();
                }
            }
            else
            {
                Selection.Clear();
            }

            UnityEditor.Selection.activeObject = null;
        }

        void OnSelectedElementDetachedFromPanel(DetachFromPanelEvent evt)
        {
            RemoveFromSelectionNoUndoRecord(evt.target as ISelectableGraphElement);
        }

        public virtual void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.target is GraphView || evt.target is Node)
            {
                evt.menu.AppendAction("Cut", (a) => { CutSelectionCallback(); },
                    (a) => { return CanCutSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled; });
            }
            if (evt.target is GraphView || evt.target is Node)
            {
                evt.menu.AppendAction("Copy", (a) => { CopySelectionCallback(); },
                    (a) => { return CanCopySelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled; });
            }
            if (evt.target is GraphView)
            {
                evt.menu.AppendAction("Paste", (a) => { PasteCallback(); },
                    (a) => { return CanPaste ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled; });
            }
            if (evt.target is GraphView || evt.target is Node || evt.target is Edge)
            {
                evt.menu.AppendSeparator();
                evt.menu.AppendAction("Delete", (a) => { DeleteSelection(); },
                    (a) => { return CanDeleteSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled; });
            }
            if (evt.target is GraphView || evt.target is Node)
            {
                evt.menu.AppendSeparator();
                evt.menu.AppendAction("Duplicate", (a) => { DuplicateSelectionCallback(); },
                    (a) => { return CanDuplicateSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled; });
                evt.menu.AppendSeparator();
            }
        }

        protected override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            base.ExecuteDefaultActionAtTarget(evt);
            DisplayContextualMenu(evt);
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

        void OnContextualMenu(ContextualMenuPopulateEvent evt)
        {
            // If popping a contextual menu on a GraphElement, add the cut/copy actions.
            BuildContextualMenu(evt);
        }

        void OnEnterPanel(AttachToPanelEvent e)
        {
            base.OnEnterPanel();

            if (IsReframable)
                panel?.visualTree.RegisterCallback<KeyDownEvent>(OnKeyDownShortcut);
        }

        void OnLeavePanel(DetachFromPanelEvent e)
        {
            if (IsReframable)
                panel.visualTree.UnregisterCallback<KeyDownEvent>(OnKeyDownShortcut);

            base.OnLeavePanel();
        }

        protected void OnKeyDownShortcut(KeyDownEvent evt)
        {
            if (!IsReframable)
                return;

            if (panel.GetCapturingElement(PointerId.mousePointerId) != null)
                return;

            switch (evt.character)
            {
                case 'a':
                    FrameAll();
                    evt.StopPropagation();
                    break;

                case 'o':
                    FrameOrigin();
                    evt.StopPropagation();
                    break;

                case '[':
                    if (FramePrev())
                        evt.StopPropagation();
                    break;

                case ']':
                    if (FrameNext())
                        evt.StopPropagation();
                    break;
            }
        }

        internal void OnValidateCommand(ValidateCommandEvent evt)
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

        internal void OnExecuteCommand(ExecuteCommandEvent evt)
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
                DeleteSelection("Delete", DeleteElementsAction.AskUser.AskUser);
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
            CollectElements(elements, elementsToCopySet, e => e.IsCopiable());

            // Also collect hovering list of nodes
            foreach (var placemat in elements.OfType<Placemat>())
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

        public void CopySelectionCallback()
        {
            var elementsToCopySet = new HashSet<GraphElement>();

            CollectCopyableGraphElements(Selection.OfType<GraphElement>(), elementsToCopySet);

            string data = SerializeGraphElements(elementsToCopySet);

            if (!string.IsNullOrEmpty(data))
            {
                Clipboard = data;
            }
        }

        public void CutSelectionCallback()
        {
            CopySelectionCallback();
            DeleteSelection("Cut");
        }

        public void PasteCallback()
        {
            UnserializeAndPasteOperation("Paste", Clipboard);
        }

        public void DuplicateSelectionCallback()
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

        public void AddElement(GraphElement graphElement)
        {
            int newLayer = graphElement.Layer;
            if (!m_ContainerLayers.ContainsKey(newLayer))
            {
                AddLayer(newLayer);
            }
            GetLayer(newLayer).Add(graphElement);

            // Attempt to restore selection on the new element if it
            // was previously selected (same GUID).
            RestorePersitentSelectionForElement(graphElement);
        }

        public void RemoveElement(GraphElement graphElement)
        {
            graphElement.RemoveFromHierarchy();
        }

        public void DeleteSelection(string operationName = "Delete", DeleteElementsAction.AskUser askUser = DeleteElementsAction.AskUser.DontAskUser)
        {
            IGTFGraphElementModel[] elementsToRemove = Selection.Cast<GraphElement>()
                .Select(x => x.Model)
                .Where(m => m != null).ToArray(); // 'this' has no model
            m_Store.Dispatch(new DeleteElementsAction(operationName, askUser, elementsToRemove));
        }

        public void DeleteElements(IEnumerable<GraphElement> elementsToRemove)
        {
            m_ElementsToRemove.Clear();
            foreach (GraphElement element in elementsToRemove)
                m_ElementsToRemove.Add(element);

            List<GraphElement> elementsToRemoveList = m_ElementsToRemove;
            if (GraphViewChangedCallback != null)
            {
                elementsToRemoveList = GraphViewChangedCallback(m_GraphViewChange).elementsToRemove;
            }

            foreach (GraphElement element in elementsToRemoveList)
            {
                RemoveElement(element);
            }
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

        public bool FramePrev()
        {
            if (contentViewContainer.childCount == 0)
                return false;

            List<GraphElement> childrenList = GraphElements.ToList().Where(e => e.IsSelectable() && !(e is Edge)).OrderByDescending(e => e.controlid).ToList();
            FramePrevNext(childrenList);
            return true;
        }

        public bool FrameNext()
        {
            if (contentViewContainer.childCount == 0)
                return false;

            List<GraphElement> childrenList = GraphElements.ToList().Where(e => e.IsSelectable() && !(e is Edge)).OrderBy(e => e.controlid).ToList();
            FramePrevNext(childrenList);
            return true;
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

        // TODO: Do we limit to GraphElements or can we tab through ISelectable's?
        void FramePrevNext(List<GraphElement> childrenList)
        {
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

            if (m_FrameAnimate)
            {
                // TODO Animate framing
                // RMAnimation animation = new RMAnimation();
                // parent.Animate(parent)
                //       .Lerp(new string[] {"m_Scale", "m_Translation"},
                //             new object[] {parent.scale, parent.translation},
                //             new object[] {frameScaling, frameTranslation}, 0.08f);
            }
            else
            {
                Matrix4x4.TRS(frameTranslation, Quaternion.identity, frameScaling);

                UpdateViewTransform(frameTranslation, frameScaling);
            }

            contentViewContainer.MarkDirtyRepaint();

            UpdatePersistedViewTransform();
        }

        public virtual Rect CalculateRectToFitAll(VisualElement container)
        {
            Rect rectToFit = container.layout;
            bool reachedFirstChild = false;

            GraphElements.ForEach(ge =>
            {
                if (ge is Edge)
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

        public static void CalculateFrameTransform(Rect rectToFit, Rect clientRect, int border, out Vector3 frameTranslation, out Vector3 frameScaling)
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
            zoomLevel = Mathf.Clamp(zoomLevel, ContentZoomer.DefaultMinScale, 1.0f);

            var transform = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(zoomLevel, zoomLevel, 1.0f));

            var edge = new Vector2(clientRect.width, clientRect.height);
            var origin = new Vector2(0, 0);

            var r = new Rect
            {
                min = origin,
                max = edge
            };

            var parentScale = new Vector3(transform.GetColumn(0).magnitude,
                transform.GetColumn(1).magnitude,
                transform.GetColumn(2).magnitude);
            Vector2 offset = r.center - (rectToFit.center * parentScale.x);

            // Update output values before leaving
            frameTranslation = new Vector3(offset.x, offset.y, 0.0f);
            frameScaling = parentScale;

            GUI.matrix = m;
        }

        public virtual Blackboard GetBlackboard()
        {
            return null;
        }

        public virtual void ReleaseBlackboard(Blackboard toRelease)
        {
        }

        protected virtual PlacematContainer CreatePlacematContainer()
        {
            return new PlacematContainer(this);
        }
    }
}
