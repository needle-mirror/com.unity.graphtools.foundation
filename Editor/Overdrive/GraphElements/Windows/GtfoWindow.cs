using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public abstract class GtfoWindow : GraphViewEditorWindow, IHasCustomMenu
    {
        public enum OpenMode { Open, OpenAndFocus }

        const string k_StyleSheetPath = PackageTransitionHelper.AssetPath + "VisualScripting/Editor/Views/Templates/";
        const int k_IdleTimeBeforeCompilationMs = 1000;
        const int k_IdleTimeBeforeCompilationMsPlayMode = 1000;
        public const string k_CompilationPendingClassName = "compilationPending";

        static int s_LastFocusedEditor = -1;

        [SerializeField]
        GameObject m_BoundObject;

        [SerializeField]
        List<OpenedGraph> m_PreviousGraphModels;

        [SerializeField]
        List<string> m_BlackboardExpandedRowStates;

        [SerializeField]
        List<string> m_ElementModelsToSelectUponCreation;

        [SerializeField]
        LockTracker m_LockTracker = new LockTracker();

        [SerializeField]
        bool m_TracingEnabled;

        ShortcutHandler m_ShortcutHandler;

        public PluginRepository PluginRepository { get; private set; }

        ErrorToolbar m_ErrorToolbar;

        Node m_ElementShownInSidePanel;

        Unity.Properties.UI.PropertyElement m_SidePanelPropertyElement;

        CompilationTimer m_CompilationTimer;

        bool m_Focused;

        public override IEnumerable<GraphView> GraphViews
        {
            get { yield return GraphView; }
        }

        public new GtfoGraphView GraphView => base.GraphView as GtfoGraphView;

        public GameObject BoundObject => m_BoundObject;

        public List<OpenedGraph> PreviousGraphModels => m_PreviousGraphModels;

        public bool TracingEnabled
        {
            get => m_TracingEnabled;
            set => m_TracingEnabled = value;
        }

        // TODO: Until serialization/persistent data is brought back into VisualElements, we need
        // a place for keeping otherwise-non serializable data, like blackboard related data (expanded/selected states, size, etc.)
        // Note that all this data is indirectly used via the Editor Data Model and should someday have its own
        // local implementation (e.g. directly in the Blackboard)
        public List<string> BlackboardExpandedRowStates => m_BlackboardExpandedRowStates;

        public List<string> ElementModelsToSelectUponCreation => m_ElementModelsToSelectUponCreation;

        public string LastGraphFilePath => m_LastGraphFilePath;

        public IEditorDataModel DataModel { get; protected set; }

        public bool RefreshUIDisabled { private get; set; }

        // PF setter is never used. Maybe useless?
        bool Locked
        {
            get => Store?.GetState().AssetModel != null && m_LockTracker.IsLocked;
            set => m_LockTracker.IsLocked = value;
        }

        protected GtfoWindow()
        {
            s_LastFocusedEditor = GetInstanceID();
            m_CompilationTimer = new CompilationTimer();
        }

        protected abstract BlankPage CreateBlankPage();

        protected abstract MainToolbar CreateMainToolbar();

        protected abstract ErrorToolbar CreateErrorToolbar();

        protected abstract GtfoGraphView CreateGraphView();

        public virtual void SetBoundObject(GameObject boundObject)
        {
            m_BoundObject = boundObject;
        }

        public override void UnloadGraph()
        {
            base.UnloadGraph();
            GraphView.UnloadGraph();
            MainToolbar.UpdateUI();
        }

        protected virtual void StoreOnStateChanged()
        {
            var editorDataModel = Store.GetState().EditorDataModel;

            UpdateFlags currentUpdateFlags = editorDataModel.UpdateFlags;
            if (currentUpdateFlags == 0)
                return;

            if (currentUpdateFlags.HasFlag(UpdateFlags.UpdateView))
            {
                foreach (var model in editorDataModel.ModelsToUpdate)
                {
                    var ui = model.GetUI<IGraphElement>(m_GraphView);
                    ui?.UpdateFromModel();
                }
                editorDataModel.ClearModelsToUpdate();
                return;
            }

            var graphModel = Store.GetState()?.CurrentGraphModel;
            m_LastGraphFilePath = graphModel?.GetAssetPath();

            if (currentUpdateFlags.HasFlag(UpdateFlags.RequestCompilation))
            {
                if (!currentUpdateFlags.HasFlag(UpdateFlags.CompilationResult))
                {
                    m_CompilationTimer.Restart(editorDataModel);
                    m_CompilationPendingLabel.EnableInClassList(k_CompilationPendingClassName, true);
                    // Register
                    var stencil = Store.GetState().CurrentGraphModel?.Stencil;
                    if (stencil != null)
                    {
                        var plugins = stencil.GetCompilationPluginHandlers(GetCompilationOptions());
                        PluginRepository.RegisterPlugins(plugins);
                        stencil.OnCompilationStarted(graphModel);
                    }
                }
            }

            // The GraphGeometry part must happen BEFORE the GraphTopology part
            // When the onboarding page is displayed before loading a graph, we need first to insert the graphview in
            // the hierarchy (UpdateGraphContainer) then create the graph itself (UpdateTopology)
            // Fixes VSB-257: edge bubbles rely on a specific event order (AttachedToPanelEvent must occur early enough or the
            // bubble won't get attached)
            if (currentUpdateFlags.HasFlag(UpdateFlags.GraphGeometry))
            {
                UpdateGraphContainer();
                m_BlankPage.UpdateUI();
                m_MainToolbar.UpdateUI();
            }

            if (currentUpdateFlags.HasFlag(UpdateFlags.GraphTopology))
            {
                if (graphModel != null)
                {
                    if (!currentUpdateFlags.HasFlag(UpdateFlags.CompilationResult))
                    {
                        m_CompilationTimer.Restart(editorDataModel);
                        m_CompilationPendingLabel.EnableInClassList(k_CompilationPendingClassName, true);

                        var stencil = Store.GetState().CurrentGraphModel?.Stencil;
                        stencil?.OnCompilationStarted(graphModel);
                    }

                    GraphView.NotifyTopologyChange(graphModel);
                }

                // A topology change should update everything.
                GraphView.UpdateTopology();
                currentUpdateFlags |= UpdateFlags.All;
            }

            if (currentUpdateFlags.HasFlag(UpdateFlags.CompilationResult))
            {
                UpdateCompilationErrorsDisplay(Store.GetState());
            }

            Store?.GetState()?.CurrentGraphModel?.CheckIntegrity(Verbosity.Errors);

            if (graphModel != null && graphModel.LastChanges.ElementsToAutoAlign.Any())
            {
                var elementsToAlign = graphModel.LastChanges.ElementsToAutoAlign
                    .Select(n => n.GetUI(m_GraphView));
                m_GraphView.schedule.Execute(() =>
                {
                    GraphView.AlignGraphElements(elementsToAlign);

                    // Black magic counter spell to the curse cast by WindowsDropTargetImpl::DragPerformed.
                    // Basically our scheduled alignment gets called right in the middle of a DragExit
                    //      (yes DragExit even though the Drag was performed properly, Look at WindowsDropTargetImpl::DragPerformed you'll understand...)
                    // DragExit calls Application::TickTimer() in the middle of its execution, letting our scheduled task run
                    // right after the TickTimer() resumes, since we're supposedly doing a DragExit (so a drag cancel) it Undoes the CurrentGroup
                    // since we don't want our scheduled task to be canceled, we do the following
                    Undo.IncrementCurrentGroup();
                });
            }
        }

        protected abstract Dictionary<Event, ShortcutDelegate> GetShortcutDictionary();

        protected virtual void SetupWindow()
        {
            rootVisualElement.Add(m_MainToolbar);
            // AddTracingTimeline();
            rootVisualElement.Add(m_GraphContainer);
            if (m_ErrorToolbar != null)
                m_GraphView.Add(m_ErrorToolbar);
        }

        public void ShowNodeInSidePanel(ISelectableGraphElement selectable, bool show)
        {
            if (!(selectable is Node node) ||
                !((selectable as IGraphElement).Model is INodeModel nodeModel) || !show)
            {
                m_ElementShownInSidePanel = null;
                m_SidePanelPropertyElement.ClearTarget();
                m_SidePanelTitle.text = "Node Inspector";
            }
            else
            {
                m_ElementShownInSidePanel = node;

                m_SidePanelTitle.text = (m_ElementShownInSidePanel?.NodeModel as IHasTitle)?.Title ??
                    "Node Inspector";

                // TODO ugly, see matching hack to get the internal Root property in the typeReferenceInspector
                m_SidePanelPropertyElement.userData = nodeModel;
                m_SidePanelPropertyElement.SetTarget(nodeModel is IPropertyVisitorNodeTarget target ? target.Target : nodeModel);
            }
        }

        public void ClearNodeInSidePanel()
        {
            ShowNodeInSidePanel(m_ElementShownInSidePanel, false);
        }

        public virtual void AddItemsToMenu(GenericMenu menu)
        {
            var disabled = Store?.GetState().CurrentGraphModel == null;

            m_LockTracker.AddItemsToMenu(menu, disabled);
        }

        public void AdjustWindowMinSize(Vector2 size)
        {
            // Set the window min size from the graphView, adding the menu bar height
            minSize = new Vector2(size.x, size.y + m_MainToolbar.layout.height);
        }

        protected void OnCompilationRequest(RequestCompilationOptions options)
        {
            var compilationOptions = GetCompilationOptions();

            // Register
            var graphModel = Store.GetState().CurrentGraphModel;

            if (graphModel == null || graphModel.Stencil == null)
                return;

            var plugins = graphModel.Stencil.GetCompilationPluginHandlers(compilationOptions);
            PluginRepository.RegisterPlugins(plugins);

            ITranslator translator = graphModel.Stencil.CreateTranslator();
            if (!translator.SupportsCompilation())
                return;

            if (options == RequestCompilationOptions.SaveGraph)
                AssetDatabase.SaveAssets();

            CompilationResult r = graphModel.Compile(translator);
            if (Store?.GetState()?.CompilationResultModel is CompilationResultModel compilationResultModel) // TODO: could have disappeared during the await
            {
                compilationResultModel.lastResult = r;
                OnCompilationDone(graphModel, compilationOptions, r);
            }
        }

        protected CompilationOptions GetCompilationOptions()
        {
            CompilationOptions compilationOptions = EditorApplication.isPlaying
                ? CompilationOptions.LiveEditing
                : CompilationOptions.Default;

            if (TracingEnabled)
                compilationOptions |= CompilationOptions.Tracing;
            return compilationOptions;
        }

        public void OnCompilationDone(IGraphModel vsGraphModel, CompilationOptions options, CompilationResult results)
        {
            if (!this)
            {
                // Should not happen, but it did, so...
                Debug.LogWarning("A destroyed GtfoWindow still has an OnCompilationDone callback registered.");
                return;
            }

            var state = Store.GetState();

            UpdateCompilationErrorsDisplay(state);

            if (results != null && results.errors.Count == 0)
            {
                // TODO : Add delegate to register to compilation Done
//                VSCompilationService.NotifyChange((ISourceProvider)vsGraphModel.assetModel);
            }
        }

        public virtual void UpdateCompilationErrorsDisplay(State state)
        {
            m_ErrorToolbar.Update();
        }

        void OnLockStateChanged(bool locked)
        {
            // Make sure that upon unlocking, any selection change is updated
            if (!locked)
                OnGlobalSelectionChange();
        }

        // DO NOT name this one "OnSelectionChange", which is a magical unity function name
        // and would automatically call this method when the selection changes.
        // we want more granular control and register it manually
        void OnGlobalSelectionChange()
        {
            // if we're in Locked mode, keep current selection
            if (Locked)
                return;

            foreach (var onboardingProvider in m_BlankPage.OnboardingProviders)
            {
                if (onboardingProvider.GetGraphAndObjectFromSelection(this, Selection.activeObject, out var selectedAssetPath, out GameObject boundObject))
                {
                    SetCurrentSelection(selectedAssetPath, OpenMode.Open, boundObject);
                    return;
                }
            }

            // selection is a GraphAssetModel
            var semanticGraph = Selection.activeObject as GraphAssetModel;
            Object selectedObject = semanticGraph;
            if (semanticGraph != null)
            {
                SetCurrentSelection(AssetDatabase.GetAssetPath(selectedObject), OpenMode.Open);
            }
        }

        public void SetCurrentSelection(string graphAssetFilePath, OpenMode mode, GameObject boundObject = null)
        {
            var windows = (GtfoWindow[])Resources.FindObjectsOfTypeAll(typeof(GtfoWindow));

            // Only the last focused editor should try to answer a change to the current selection.
            if (s_LastFocusedEditor != GetInstanceID() && windows.Length > 1)
                return;

            var editorDataModel = Store.GetState().EditorDataModel;
            if (editorDataModel == null)
                return;
            var curBoundObject = editorDataModel.BoundObject;

            if (AssetDatabase.LoadAssetAtPath<GraphAssetModel>(graphAssetFilePath))
            {
                // don't load if same graph and same bound object
                if (Store.GetState() != null && Store.GetState().AssetModel != null &&
                    graphAssetFilePath == LastGraphFilePath &&
                    curBoundObject == boundObject)
                    return;
            }

            // If there is not graph asset, unload the current one.
            if (string.IsNullOrWhiteSpace(graphAssetFilePath))
            {
                return;
            }

            // Load this graph asset.
            Store.Dispatch(new LoadGraphAssetAction(graphAssetFilePath, boundObject));
            m_GraphView.FrameAll();

            if (mode != OpenMode.OpenAndFocus)
                return;
            // Check if an existing VSE already has this asset, if yes give it the focus.
            foreach (var window in windows)
            {
                if (window.GetCurrentAssetPath() == graphAssetFilePath)
                {
                    window.Focus();
                    return;
                }
            }
        }

        string GetCurrentAssetPath()
        {
            var asset = Store.GetState().AssetModel;
            return asset == null ? null : AssetDatabase.GetAssetPath(asset as ScriptableObject);
        }

        protected override void OnEnable()
        {
            if (m_PreviousGraphModels == null)
                m_PreviousGraphModels = new List<OpenedGraph>();

            if (m_BlackboardExpandedRowStates == null)
                m_BlackboardExpandedRowStates = new List<string>();

            if (m_ElementModelsToSelectUponCreation == null)
                m_ElementModelsToSelectUponCreation = new List<string>();

            // PF FIXME Stylesheet
            rootVisualElement.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(k_StyleSheetPath + "VSEditor.uss"));

            rootVisualElement.RegisterCallback<MouseMoveEvent>(_ =>
            {
                if (m_CompilationTimer.IsRunning)
                    m_CompilationTimer.Restart(Store.GetState().EditorDataModel);
            });

            rootVisualElement.Clear();
            rootVisualElement.style.overflow = Overflow.Hidden;
            rootVisualElement.pickingMode = PickingMode.Ignore;
            rootVisualElement.style.flexDirection = FlexDirection.Column;
            rootVisualElement.name = "vseRoot";

            // Create the store.
            base.OnEnable();

            m_GraphContainer = new VisualElement { name = "graphContainer" };
            m_GraphView = CreateGraphView();
            m_MainToolbar = CreateMainToolbar();
            m_ErrorToolbar = CreateErrorToolbar();
            m_BlankPage = CreateBlankPage();

            SetupWindow();

            m_CompilationPendingLabel = new Label("Compilation Pending"){name = "compilationPendingLabel"};

            m_SidePanel = new VisualElement(){name = "sidePanel"};
            m_SidePanelTitle = new Label();
            m_SidePanel.Add(m_SidePanelTitle);
            m_SidePanelPropertyElement = new Unity.Properties.UI.PropertyElement {name = "sidePanelInspector"};
            m_SidePanelPropertyElement.OnChanged += (element, path) =>
            {
                if (m_ElementShownInSidePanel.Model is IPropertyVisitorNodeTarget nodeTarget2)
                {
                    Store.Dispatch(new UpdateModelPropertyValueAction(m_ElementShownInSidePanel.Model, path, m_SidePanelPropertyElement.GetValue<object>(path)));
                    nodeTarget2.Target = element.GetTarget<object>();
                }
                else
                    Store.Dispatch(new UpdateModelPropertyValueAction(m_ElementShownInSidePanel.Model, path, m_SidePanelPropertyElement.GetValue<object>(path)));

                m_ElementShownInSidePanel?.NodeModel.DefineNode();
                m_ElementShownInSidePanel?.UpdateFromModel();
                Store.ForceRefreshUI(UpdateFlags.RequestCompilation);
            };
            m_SidePanel.Add(m_SidePanelPropertyElement);
            ShowNodeInSidePanel(null, false);

            m_GraphContainer.Add(m_GraphView);
            m_GraphContainer.Add(m_SidePanel);

            m_ShortcutHandler = new ShortcutHandler(GetShortcutDictionary());

            rootVisualElement.parent.AddManipulator(m_ShortcutHandler);

            Store.StateChanged += StoreOnStateChanged;
            Undo.undoRedoPerformed += UndoRedoPerformed;

            rootVisualElement.RegisterCallback<AttachToPanelEvent>(OnEnterPanel);
            rootVisualElement.RegisterCallback<DetachFromPanelEvent>(OnLeavePanel);
            // that will be true when the window is restored during the editor startup, so OnEnterPanel won't be called later
            if (rootVisualElement.panel != null)
                OnEnterPanel(null);

            titleContent = new GUIContent("Visual Script");

            // After a domain reload, all loaded objects will get reloaded and their OnEnable() called again
            // It looks like all loaded objects are put in a deserialization/OnEnable() queue
            // the previous graph's nodes/edges/... might be queued AFTER this window's OnEnable
            // so relying on objects to be loaded/initialized is not safe
            // hence, we need to defer the loading action
            rootVisualElement.schedule.Execute(() =>
            {
                if (!String.IsNullOrEmpty(LastGraphFilePath))
                {
                    try
                    {
                        Store.Dispatch(new LoadGraphAssetAction(LastGraphFilePath, boundObject: m_BoundObject, loadType: LoadGraphAssetAction.Type.KeepHistory));
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
                }
                else             // will display the blank page. not needed otherwise as the LoadGraphAsset reducer will refresh
                    Store.ForceRefreshUI(UpdateFlags.All);
            }).ExecuteLater(0);

            m_LockTracker.lockStateChanged.AddListener(OnLockStateChanged);

            PluginRepository = new PluginRepository(Store, this);

            EditorApplication.playModeStateChanged += OnEditorPlayModeStateChanged;
            EditorApplication.pauseStateChanged += OnEditorPauseStateChanged;
        }

        void OnEditorPlayModeStateChanged(PlayModeStateChange playMode)
        {
            MainToolbar.UpdateUI();
        }

        void OnEditorPauseStateChanged(PauseState pauseState)
        {
            MainToolbar.UpdateUI();
        }

        protected override void OnDisable()
        {
            // ReSharper disable once DelegateSubtraction
            Undo.undoRedoPerformed -= UndoRedoPerformed;

            if (rootVisualElement != null)
            {
                if (m_ShortcutHandler != null)
                    rootVisualElement.parent.RemoveManipulator(m_ShortcutHandler);
            }

            base.OnDisable();

            PluginRepository?.Dispose();

            EditorApplication.playModeStateChanged -= OnEditorPlayModeStateChanged;
            EditorApplication.pauseStateChanged -= OnEditorPauseStateChanged;
        }

        protected virtual void OnFocus()
        {
            m_Focused = true;

            s_LastFocusedEditor = GetInstanceID();

            if (m_Focused)
                return;

            if (rootVisualElement == null)
                return;

            if (m_ShortcutHandler != null)
                rootVisualElement.parent.AddManipulator(m_ShortcutHandler);

            // selection may have changed while Visual Scripting Editor was looking away
            OnGlobalSelectionChange();
        }

        protected virtual void OnLostFocus()
        {
            m_Focused = false;
        }

        protected virtual void Update()
        {
            if (Store == null)
                return;

            Store.Update();

            if (DataModel.Preferences.GetBool(BoolPref.AutoRecompile) &&
                m_CompilationTimer.ElapsedMilliseconds >= (EditorApplication.isPlaying
                                                           ? k_IdleTimeBeforeCompilationMsPlayMode
                                                           : k_IdleTimeBeforeCompilationMs))
            {
                m_CompilationTimer.Stop(DataModel);
                m_CompilationPendingLabel.EnableInClassList(k_CompilationPendingClassName, false);

                OnCompilationRequest(RequestCompilationOptions.Default);
            }
        }

        void UndoRedoPerformed()
        {
            if (!RefreshUIDisabled)
                Store.ForceRefreshUI(UpdateFlags.All);
        }

        void OnEnterPanel(AttachToPanelEvent e)
        {
            rootVisualElement.parent.AddManipulator(m_ShortcutHandler);
            Selection.selectionChanged += OnGlobalSelectionChange;
            OnGlobalSelectionChange();
        }

        void OnLeavePanel(DetachFromPanelEvent e)
        {
            rootVisualElement.parent.RemoveManipulator(m_ShortcutHandler);
            // ReSharper disable once DelegateSubtraction
            Selection.selectionChanged -= OnGlobalSelectionChange;
        }
    }
}
