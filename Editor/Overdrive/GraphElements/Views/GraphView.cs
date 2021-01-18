using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive
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

    public abstract class GraphView : GraphViewBridge, ISelection
    {
        public static readonly string ussClassName = "ge-graph-view";

        // Layer class. Used for queries below.
        public class Layer : VisualElement {}

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
        //[MovedFrom(false, "Unity.GraphElements", "Unity.GraphTools.Foundation.Overdrive.Editor")]
        [MovedFrom(false, "UnityEditor.GraphToolsFoundation.Overdrive.GraphElements")]
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

        // PF FIXME: we should be able to remove this.
        GraphViewEditorWindow m_Window;

        int m_SavedSelectionVersion;

        PersistedSelection m_PersistedSelection;

        GraphViewUndoRedoSelection m_GraphViewUndoRedoSelection;

        bool m_FrameAnimate = false;

        readonly Dictionary<int, Layer> m_ContainerLayers = new Dictionary<int, Layer>();

        PlacematContainer m_PlacematContainer;

        IVisualElementScheduledItem m_OnTimerTicker;

        PersistedViewTransform m_PersistedViewTransform;

        UQueryState<GraphElement> m_AllGraphElements;

        ContextualMenuManipulator m_ContextualMenuManipulator;
        ContentZoomer m_Zoomer;

        AutoSpacingHelper m_AutoSpacingHelper;
        AutoAlignmentHelper m_AutoAlignmentHelper;

        float m_MinScale = ContentZoomer.DefaultMinScale;
        float m_MaxScale = ContentZoomer.DefaultMaxScale;
        float m_MaxScaleOnFrame = 1.0f;
        float m_ScaleStep = ContentZoomer.DefaultScaleStep;
        float m_ReferenceScale = ContentZoomer.DefaultReferenceScale;

        Blackboard m_Blackboard;

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

        float MinScale => m_MinScale;

        float MaxScale => m_MaxScale;

        string m_Clipboard = string.Empty;

        public Store Store => m_Store;

        public GraphViewEditorWindow Window => m_Window;

        public UQueryState<GraphElement> GraphElements { get; }

        public UQueryState<Node> Nodes { get; }

        public UQueryState<Port> Ports;

        public UQueryState<Edge> Edges { get; }

        public UQueryState<StickyNote> Stickies { get; }

        public ViewTransformChanged ViewTransformChangedCallback { get; set; }

        public virtual bool SupportsWindowedBlackboard => false;

        protected bool PersistedSelectionRestoreEnabled { get; private set; }

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

        public virtual IEnumerable<IHighlightable> Highlightables
        {
            get
            {
                IEnumerable<IHighlightable> elements = GraphElements.ToList().OfType<IHighlightable>();

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
                return elements.Concat(Blackboard?.Highlightables ?? Enumerable.Empty<IHighlightable>());
            }
        }

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
        public VisualElement BadgesParent { get; }

        bool IsReframable { get; }

        public Blackboard Blackboard
        {
            get
            {
                if (m_Blackboard == null && m_Store.State?.BlackboardGraphModel != null)
                {
                    m_Blackboard = GraphElementFactory.CreateUI<Blackboard>(this, m_Store, m_Store.State?.BlackboardGraphModel);
                    m_Blackboard?.AddToGraphView(this);
                }

                return m_Blackboard;
            }
        }

        internal PositionDependenciesManager PositionDependenciesManager { get; }

        protected GraphView(GraphViewEditorWindow window, Store store)
        {
            m_Window = window;
            m_Store = store;

            PersistedSelectionRestoreEnabled = true;

            AddToClassList(ussClassName);

            this.SetRenderHintsForGraphView();

            Selection = new List<ISelectableGraphElement>();

            GraphViewContainer = new VisualElement() {name = "graph-view-container"};
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

            IsReframable = true;
            focusable = true;

            RegisterCallback<ValidateCommandEvent>(OnValidateCommand);
            RegisterCallback<ExecuteCommandEvent>(OnExecuteCommand);
            RegisterCallback<AttachToPanelEvent>(OnEnterPanel);
            RegisterCallback<DetachFromPanelEvent>(OnLeavePanel);

            ContextualMenuManipulator = new ContextualMenuManipulator(BuildContextualMenu);

            PositionDependenciesManager = new PositionDependenciesManager(this, Store?.State?.Preferences);
            m_AutoAlignmentHelper = new AutoAlignmentHelper(this);
            m_AutoSpacingHelper = new AutoSpacingHelper(this);

            BadgesParent = new VisualElement { name = "iconsParent"};
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
                this.HighlightGraphElements();
            }

            UnityEditor.Selection.activeObject = null;
        }

        void OnSelectedElementDetachedFromPanel(DetachFromPanelEvent evt)
        {
            RemoveFromSelectionNoUndoRecord(evt.target as ISelectableGraphElement);
        }

        public virtual void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
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

                m_Store.Dispatch(new CreatePlacematAction(new Rect(graphPosition.x, graphPosition.y, 200, 200)));
            });

            if (Selection.Any())
            {
                var nodesAndNotes = Selection.OfType<GraphElement>().Where(e => (e is Node || e is StickyNote)).ToList();
                evt.menu.AppendAction("Create Placemat Under Selection", _ =>
                {
                    Rect bounds = new Rect();
                    if (Placemat.ComputeElementBounds(ref bounds, nodesAndNotes))
                    {
                        m_Store.Dispatch(new CreatePlacematAction(bounds));
                    }
                }, nodesAndNotes.Count == 0 ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);

                /* Actions on selection */

                evt.menu.AppendSeparator();

                evt.menu.AppendAction("Align Elements/Align Items", _ =>
                {
                    m_Store.Dispatch(new AlignNodesAction(this, false));
                });

                evt.menu.AppendAction("Align Elements/Align Hierarchy", _ =>
                {
                    m_Store.Dispatch(new AlignNodesAction(this, true));
                });

                if (Selection.OfType<GraphElement>().Count(elem => !(elem is Edge) && elem.visible) > 1)
                {
                    evt.menu.AppendAction("Align Elements/Top",
                        _ => m_AutoAlignmentHelper.SendAlignAction(AutoAlignmentHelper.AlignmentReference.Top));

                    evt.menu.AppendAction("Align Elements/Bottom",
                        _ => m_AutoAlignmentHelper.SendAlignAction(AutoAlignmentHelper.AlignmentReference.Bottom));

                    evt.menu.AppendAction("Align Elements/Left",
                        _ => m_AutoAlignmentHelper.SendAlignAction(AutoAlignmentHelper.AlignmentReference.Left));

                    evt.menu.AppendAction("Align Elements/Right",
                        _ => m_AutoAlignmentHelper.SendAlignAction(AutoAlignmentHelper.AlignmentReference.Right));

                    evt.menu.AppendAction("Align Elements/Horizontal Center",
                        _ => m_AutoAlignmentHelper.SendAlignAction(AutoAlignmentHelper.AlignmentReference.HorizontalCenter));

                    evt.menu.AppendAction("Align Elements/Vertical Center",
                        _ => m_AutoAlignmentHelper.SendAlignAction(AutoAlignmentHelper.AlignmentReference.VerticalCenter));

                    evt.menu.AppendAction("Space Elements/Horizontal",
                        _ => m_AutoSpacingHelper.SendSpacingAction(Orientation.Horizontal));

                    evt.menu.AppendAction("Space Elements/Vertical",
                        _ => m_AutoSpacingHelper.SendSpacingAction(Orientation.Vertical));
                }

                var nodes = Selection.OfType<Node>().Select(e => e.NodeModel).ToArray();
                if (nodes.Length > 0)
                {
                    var connectedNodes = nodes
                        .Where(m => m.GetConnectedEdges().Any())
                        .ToArray();

                    evt.menu.AppendAction("Disconnect Nodes", _ =>
                    {
                        m_Store.Dispatch(new DisconnectNodeAction(connectedNodes));
                    }, connectedNodes.Length == 0 ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);

                    var ioConnectedNodes = connectedNodes.OfType<IInOutPortsNode>()
                        .Where(x => x.InputsByDisplayOrder.Any(y => y.IsConnected()) &&
                            x.OutputsByDisplayOrder.Any(y => y.IsConnected())).ToArray();

                    evt.menu.AppendAction("Bypass Nodes", _ =>
                    {
                        m_Store.Dispatch(new BypassNodesAction(ioConnectedNodes, nodes));
                    }, ioConnectedNodes.Length == 0 ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);

                    var willDisable = nodes.Any(n => n.State == ModelState.Enabled);
                    evt.menu.AppendAction(willDisable ? "Disable Nodes" : "Enable Nodes", _ =>
                    {
                        m_Store.Dispatch(new SetNodeEnabledStateAction(nodes, willDisable ? ModelState.Disabled : ModelState.Enabled));
                    });
                }

                var graphElementModels = Selection.OfType<GraphElement>().Select(e => e.Model).ToList();
                if (graphElementModels.Count == 2)
                {
                    // PF: FIXME check conditions correctly for this actions (exclude single port nodes, check if already connected).
                    if (graphElementModels.FirstOrDefault(x => x is IEdgeModel) is IEdgeModel edgeModel &&
                        graphElementModels.FirstOrDefault(x => x is IInOutPortsNode) is IInOutPortsNode nodeModel)
                    {
                        evt.menu.AppendAction("Insert Node on Edge", _ => m_Store.Dispatch(new SplitEdgeAndInsertExistingNodeAction(edgeModel, nodeModel)),
                            eventBase => DropdownMenuAction.Status.Normal);
                    }
                }

                var variableNodes = nodes.OfType<IVariableNodeModel>().ToArray();
                if (variableNodes.Length > 0)
                {
                    // TODO JOCE We might want to bring the concept of Get/Set variable from VS down to GTF
                    evt.menu.AppendAction("Variable/Convert",
                        _ => m_Store.Dispatch(new ConvertVariableNodesToConstantNodesAction(variableNodes)),
                        variableNodes.Any(v => v.OutputsByDisplayOrder.Any(o => o.PortType == PortType.Data)) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

                    evt.menu.AppendAction("Variable/Itemize",
                        _ => m_Store.Dispatch(new ItemizeNodeAction(variableNodes)),
                        variableNodes.Any(v => v.OutputsByDisplayOrder.Any(o => o.PortType == PortType.Data && o.GetConnectedPorts().Count() > 1)) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                }

                var constants = nodes.OfType<IConstantNodeModel>().ToArray();
                if (constants.Length > 0)
                {
                    evt.menu.AppendAction("Constant/Convert",
                        _ => m_Store.Dispatch(new ConvertConstantNodesToVariableNodesAction(constants)), x => DropdownMenuAction.Status.Normal);

                    evt.menu.AppendAction("Constant/Itemize",
                        _ => m_Store.Dispatch(new ItemizeNodeAction(constants)),
                        constants.Any(v => v.OutputsByDisplayOrder.Any(o => o.PortType == PortType.Data && o.GetConnectedPorts().Count() > 1)) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

                    evt.menu.AppendAction("Constant/Lock",
                        _ => m_Store.Dispatch(new ToggleLockConstantNodeAction(constants)), x => DropdownMenuAction.Status.Normal);
                }

                var portals = nodes.OfType<IEdgePortalModel>().ToArray();
                if (portals.Length > 0)
                {
                    var canCreate = portals.Where(p => p.CanCreateOppositePortal()).ToArray();
                    evt.menu.AppendAction("Create Opposite Portal",
                        _ =>
                        {
                            m_Store.Dispatch(new CreateOppositePortalAction(canCreate));
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
                            m_Store.Dispatch(new ChangeElementColorAction(pickedColor, nodes, placemats));
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
                        m_Store.Dispatch(new ResetElementColorAction(nodes, placemats));
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
                        s =>
                        {
                            var e = s.GetUI<Edge>(this);
                            var outputPort = s.FromPort.GetUI<Port>(this);
                            var inputPort = s.ToPort.GetUI<Port>(this);
                            var outputNode = s.FromPort.NodeModel.GetUI<Node>(this);
                            var inputNode = s.ToPort.NodeModel.GetUI<Node>(this);
                            return (s,
                                outputPort.ChangeCoordinatesTo(outputNode.parent, outputPort.layout.center),
                                inputPort.ChangeCoordinatesTo(inputNode.parent, inputPort.layout.center));
                        }).ToList();

                    evt.menu.AppendAction("Create Portals", _ =>
                    {
                        m_Store.Dispatch(new ConvertEdgesToPortalsAction(edgeData));
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
                            menuAction => m_Store.Dispatch(new UpdateStickyNoteThemeAction(stickyNotes, menuAction.userData as string)),
                            GetThemeStatus, value);
                    }

                    foreach (var value in StickyNote.GetSizes())
                    {
                        evt.menu.AppendAction("Sticky Note Text Size/" + value,
                            menuAction => m_Store.Dispatch(new UpdateStickyNoteTextSizeAction(stickyNotes, menuAction.userData as string)),
                            GetSizeStatus, value);
                    }
                }
            }

            evt.menu.AppendSeparator();

            var models = Selection.OfType<GraphElement>().Select(e => e.Model).ToArray();

            // PF: FIXME use an Action.
            evt.menu.AppendAction("Cut", (a) => { CutSelectionCallback(); },
                CanCutSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendAction("Copy", (a) => { CopySelectionCallback(); },
                CanCopySelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            // PF: FIXME use an Action.
            evt.menu.AppendAction("Paste", (a) => { PasteCallback(); },
                CanPaste ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendSeparator();

            // PF: FIXME use an Action.
            evt.menu.AppendAction("Duplicate", (a) => { DuplicateSelectionCallback(); },
                CanDuplicateSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendAction("Delete", _ =>
            {
                m_Store.Dispatch(new DeleteElementsAction(models));
            }, CanDeleteSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            if (Unsupported.IsDeveloperBuild())
            {
                evt.menu.AppendSeparator();
                evt.menu.AppendAction("Internal/Refresh All UI", _ => m_Store.MarkStateDirty());

                if (Selection.Any())
                {
                    var selectedModels = Selection.OfType<GraphElement>().Select(e => e.Model).ToArray();

                    evt.menu.AppendAction("Internal/Refresh Selected Element(s)",
                        _ =>
                        {
                            m_Store.State.MarkChanged(selectedModels);
                        });

                    if (selectedModels.OfType<INodeModel>().Any())
                    {
                        evt.menu.AppendAction("Internal/Redefine Node",
                            action =>
                            {
                                foreach (var model in selectedModels.OfType<INodeModel>())
                                    model.DefineNode();
                            });
                    }
                }
            }
        }

        public virtual void DisplaySmartSearch(Vector2 mousePosition)
        {
            var graphPosition = contentViewContainer.WorldToLocal(mousePosition);
            var element = panel.Pick(mousePosition).GetFirstOfType<IGraphElement>();
            switch (element)
            {
                case Edge edge:
                    SearcherService.ShowEdgeNodes(Store.State, edge.EdgeModel, mousePosition, item =>
                    {
                        Store.Dispatch(new CreateNodeOnEdgeAction(edge.EdgeModel, graphPosition, item));
                    });
                    break;

                default:
                    SearcherService.ShowGraphNodes(Store.State, mousePosition, item =>
                    {
                        Store.Dispatch(new CreateNodeFromSearcherAction(graphPosition, item, new[] {GUID.Generate() }));
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

        void OnEnterPanel(AttachToPanelEvent e)
        {
            base.OnEnterPanel();
            panel?.visualTree.RegisterCallback<KeyDownEvent>(OnKeyDownShortcut);
        }

        void OnLeavePanel(DetachFromPanelEvent e)
        {
            panel.visualTree.UnregisterCallback<KeyDownEvent>(OnKeyDownShortcut);
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

        protected void OnKeyDownShortcut(KeyDownEvent evt)
        {
            if ((evt.keyCode == KeyCode.F2 && Application.platform != RuntimePlatform.OSXEditor) ||
                ((evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter) && Application.platform == RuntimePlatform.OSXEditor))
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

                    evt.StopPropagation();
                    return;
                }
            }

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

        public virtual void AddElement(GraphElement graphElement)
        {
            if (graphElement is Badge)
            {
                BadgesParent.Add(graphElement);
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

            // Attempt to restore selection on the new element if it
            // was previously selected (same GUID).
            RestorePersitentSelectionForElement(graphElement);

            graphElement.AddToGraphView(this);
        }

        public virtual void RemoveElement(GraphElement graphElement, bool unselectBeforeRemove = false)
        {
            if (unselectBeforeRemove)
            {
                graphElement.Unselect(this);
            }

            if (graphElement is Placemat placemat)
            {
                m_PlacematContainer.RemovePlacemat(placemat);
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
            m_Store.Dispatch(new DeleteElementsAction(elementsToRemove) { UndoString = operationName });
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
            zoomLevel = Mathf.Clamp(zoomLevel, MinScale, Math.Min(MaxScale, m_MaxScaleOnFrame));

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

        protected virtual PlacematContainer CreatePlacematContainer()
        {
            return new PlacematContainer(this);
        }

        public void PanToNode(GUID nodeGuid)
        {
            var graphModel = Store.State.GraphModel;

            if (!graphModel.NodesByGuid.TryGetValue(nodeGuid, out var nodeModel))
                return;

            var graphElement = nodeModel.GetUI<GraphElement>(this);
            if (graphElement == null)
                return;

            graphElement.Select(this, false);
            FrameSelection();
        }

        public virtual IEnumerable<(IVariableDeclarationModel, SerializableGUID, Vector2)> ExtractVariablesFromDroppedElements(
            IReadOnlyCollection<GraphElement> dropElements, Vector2 initialPosition)
        {
            return Enumerable.Empty<(IVariableDeclarationModel, SerializableGUID, Vector2)>();
        }

        public void AddPositionDependency(IEdgeModel model)
        {
            PositionDependenciesManager.AddPositionDependency(model);
        }

        public void RemovePositionDependency(IEdgeModel edgeModel)
        {
            PositionDependenciesManager.Remove(edgeModel.FromNodeGuid, edgeModel.ToNodeGuid);
            PositionDependenciesManager.LogDependencies();
        }

        public void AddPortalDependency(IEdgePortalModel model)
        {
            PositionDependenciesManager.AddPortalDependency(model);
        }

        public void RemovePortalDependency(IEdgePortalModel model)
        {
            PositionDependenciesManager.RemovePortalDependency(model);
            PositionDependenciesManager.LogDependencies();
        }

        public EventPropagation AlignSelection(bool follow)
        {
            Store.Dispatch(new AlignNodesAction(this, follow));
            return EventPropagation.Stop;
        }

        public virtual void StopSelectionDragger() {}
    }
}
