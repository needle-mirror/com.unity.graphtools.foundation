using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.Profiling;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public abstract class GtfoWindow : GraphViewEditorWindow, IHasCustomMenu
    {
        public enum OpenMode { Open, OpenAndFocus }

        const int k_IdleTimeBeforeCompilationMs = 1000;
        const int k_IdleTimeBeforeCompilationMsPlayMode = 1000;
        public const string compilationPendingUssClassName = "compilation-pending";

        static int s_LastFocusedEditor = -1;

        [SerializeField]
        LockTracker m_LockTracker = new LockTracker();

        uint m_LastStateVersion;

        ShortcutHandler m_ShortcutHandler;

        Node m_ElementShownInSidePanel;

        Unity.Properties.UI.PropertyElement m_SidePanelPropertyElement;

        CompilationTimer m_CompilationTimer;

        bool m_Focused;

        public ShortcutHandler ShortcutHandler
        {
            get => m_ShortcutHandler;
            set => rootVisualElement.parent.ReplaceManipulator(ref m_ShortcutHandler, value);
        }

        public bool WithSidePanel { get; set; } = true;

        public override IEnumerable<GraphView> GraphViews
        {
            get { yield return GraphView; }
        }

        public new GtfoGraphView GraphView => base.GraphView as GtfoGraphView;

        public bool RefreshUIDisabled { private get; set; }

        // PF setter is never used. Maybe useless?
        bool Locked
        {
            get => Store?.State?.AssetModel != null && m_LockTracker.IsLocked;
            set => m_LockTracker.IsLocked = value;
        }

        protected GtfoWindow()
        {
            s_LastFocusedEditor = GetInstanceID();
            m_CompilationTimer = new CompilationTimer();
        }

        public override void UnloadGraph()
        {
            base.UnloadGraph();
            GraphView.UnloadGraph();
            MainToolbar?.UpdateUI();
        }

        protected virtual IEnumerable<Type> RecompilationTriggerActions => new[]
        {
            typeof(RequestCompilationAction),
            typeof(ReorderEdgeAction),
            typeof(RenameElementAction),
            typeof(UpdateConstantNodeValueAction),
            typeof(UpdatePortConstantAction),
            typeof(UpdateModelPropertyValueAction),
            typeof(LoadGraphAssetAction),
            typeof(ChangeVariableTypeAction),
            typeof(BuildAllEditorAction),
            typeof(CreateGraphVariableDeclarationAction),
            typeof(InitializeVariableAction),
            typeof(UpdateExposedAction),
            typeof(CreateEdgeAction),
            typeof(DeleteElementsAction)
        };

        void RecompileGraphObserver(BaseAction action)
        {
            if (RecompilationTriggerActions.Contains(action.GetType()))
            {
                RecompileGraph();
            }
        }

        internal void RecompileGraph()
        {
            m_CompilationTimer.Restart(Store.State.CompilationStateComponent);
            m_CompilationPendingLabel.EnableInClassList(compilationPendingUssClassName, true);

            // Register
            var stencil = Store.State.GraphModel?.Stencil;
            if (stencil != null)
            {
                var plugins = stencil.GetCompilationPluginHandlers(GetCompilationOptions());
                Store.State.PluginRepository.RegisterPlugins(plugins, Store, this);
                stencil.OnCompilationStarted(Store.State?.GraphModel);
            }
        }

        protected abstract Dictionary<Event, ShortcutDelegate> GetShortcutDictionary();

        public void ShowNodeInSidePanel(ISelectableGraphElement selectable, bool show)
        {
            if (m_SidePanel == null)
                return;

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
            var disabled = Store.State.GraphModel == null;

            m_LockTracker.AddItemsToMenu(menu, disabled);
        }

        public void AdjustWindowMinSize(Vector2 size)
        {
            // Set the window min size from the graphView, adding the menu bar height
            minSize = new Vector2(size.x, size.y + m_MainToolbar?.layout.height ?? 0);
        }

        protected void OnCompilationRequest(RequestCompilationOptions options)
        {
            var compilationOptions = GetCompilationOptions();

            // Register
            var graphModel = Store.State.GraphModel;

            if (graphModel == null || graphModel.Stencil == null)
                return;

            var plugins = graphModel.Stencil.GetCompilationPluginHandlers(compilationOptions);
            Store.State.PluginRepository.RegisterPlugins(plugins, Store, this);

            ITranslator translator = graphModel.Stencil.CreateTranslator();
            if (!translator.SupportsCompilation())
                return;

            if (options == RequestCompilationOptions.SaveGraph)
                AssetDatabase.SaveAssets();

            CompilationResult r = translator.Compile(graphModel);
            if (Store?.State != null)
            {
                Store.State.CompilationStateComponent.m_LastResult = r;
                OnCompilationDone(graphModel, compilationOptions, r);
            }
        }

        protected virtual CompilationOptions GetCompilationOptions()
        {
            CompilationOptions compilationOptions = EditorApplication.isPlaying
                ? CompilationOptions.LiveEditing
                : CompilationOptions.Default;

            if (Store.State.TracingState.TracingEnabled)
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

            UpdateCompilationErrorsDisplay();

            if (results != null && results.errors.Count == 0)
            {
                // TODO : Add delegate to register to compilation Done
//                VSCompilationService.NotifyChange((ISourceProvider)vsGraphModel.assetModel);
            }
        }

        public virtual void UpdateCompilationErrorsDisplay()
        {
            GraphView.DisplayCompilationErrors();
        }

        void OnLockStateChanged(bool locked)
        {
            // Make sure that upon unlocking, any selection change is updated
            if (!locked)
                OnGlobalSelectionChange();
        }

        protected abstract bool CanHandleAssetType(GraphAssetModel asset);

        // DO NOT name this one "OnSelectionChange", which is a magical unity function name
        // and would automatically call this method when the selection changes.
        // we want more granular control and register it manually
        void OnGlobalSelectionChange()
        {
            // if we're in Locked mode, keep current selection
            if (Locked)
                return;

            foreach (var onboardingProvider in m_BlankPage?.OnboardingProviders ?? Enumerable.Empty<IOnboardingProvider>())
            {
                if (onboardingProvider.GetGraphAndObjectFromSelection(this, Selection.activeObject, out var selectedAssetPath, out GameObject boundObject))
                {
                    SetCurrentSelection(selectedAssetPath, OpenMode.Open, boundObject);
                    return;
                }
            }

            var graph = Selection.activeObject as IGraphAssetModel;
            if (CanHandleAssetType(graph as GraphAssetModel) && graph is Object selectedObject && selectedObject)
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

            // PF FIXME load correct asset type (not GraphAssetModel)
            if (AssetDatabase.LoadAssetAtPath(graphAssetFilePath, typeof(GraphAssetModel)))
            {
                var currentOpenedGraph = Store.State?.WindowState.CurrentGraph ?? default;
                // don't load if same graph and same bound object
                if (Store.State?.AssetModel != null &&
                    graphAssetFilePath == currentOpenedGraph.GraphAssetModelPath &&
                    currentOpenedGraph.BoundObject == boundObject)
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
            var asset = Store.State.AssetModel;
            return asset == null ? null : AssetDatabase.GetAssetPath(asset as Object);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            Store.RegisterObserver(RecompileGraphObserver, asPostActionObserver: true);

            rootVisualElement.RegisterCallback<MouseMoveEvent>(_ =>
            {
                if (m_CompilationTimer.IsRunning)
                    m_CompilationTimer.Restart(Store.State.CompilationStateComponent);
            });

            m_CompilationPendingLabel = new Label("Compilation Pending"){name = "compilationPendingLabel"};

            if (WithSidePanel)
            {
                m_SidePanel = new VisualElement { name = "sidePanel" };
                m_SidePanelTitle = new Label();
                m_SidePanel.Add(m_SidePanelTitle);
                m_SidePanelPropertyElement = new Unity.Properties.UI.PropertyElement { name = "sidePanelInspector" };
                m_SidePanelPropertyElement.OnChanged += (element, path) =>
                {
                    // PF FIXME: OnChanged is not only called as a direct result of a user action.
                    // It is called as a result of any change to one of the property. It results in a
                    // Multiple actions dispatched during the same frame (previous one was CreateOppositePortalAction), current: UpdateModelPropertyValueAction
                    // Repro: Select a portal and create opposite portal.
                    if (m_ElementShownInSidePanel.Model is IPropertyVisitorNodeTarget nodeTarget2)
                    {
                        Store.Dispatch(new UpdateModelPropertyValueAction(m_ElementShownInSidePanel.Model, path, m_SidePanelPropertyElement.GetValue<object>(path)));
                        nodeTarget2.Target = element.GetTarget<object>();
                    }
                    else
                        Store.Dispatch(new UpdateModelPropertyValueAction(m_ElementShownInSidePanel.Model, path, m_SidePanelPropertyElement.GetValue<object>(path)));
                };
                m_SidePanel.Add(m_SidePanelPropertyElement);
                ShowNodeInSidePanel(null, false);
            }

            if (m_SidePanel != null)
                m_GraphContainer.Add(m_SidePanel);

            ShortcutHandler = new ShortcutHandler(GetShortcutDictionary());

            Undo.undoRedoPerformed += UndoRedoPerformed;

            rootVisualElement.RegisterCallback<AttachToPanelEvent>(OnEnterPanel);
            rootVisualElement.RegisterCallback<DetachFromPanelEvent>(OnLeavePanel);
            // that will be true when the window is restored during the editor startup, so OnEnterPanel won't be called later
            if (rootVisualElement.panel != null)
                OnEnterPanel(null);

            titleContent = new GUIContent("Visual Script");

            m_LockTracker.lockStateChanged.AddListener(OnLockStateChanged);

            EditorApplication.playModeStateChanged += OnEditorPlayModeStateChanged;
            EditorApplication.pauseStateChanged += OnEditorPauseStateChanged;
        }

        void OnEditorPlayModeStateChanged(PlayModeStateChange playMode)
        {
            MainToolbar?.UpdateUI();
        }

        void OnEditorPauseStateChanged(PauseState pauseState)
        {
            MainToolbar?.UpdateUI();
        }

        protected override void OnDisable()
        {
            // ReSharper disable once DelegateSubtraction
            Undo.undoRedoPerformed -= UndoRedoPerformed;

            base.OnDisable();

            EditorApplication.playModeStateChanged -= OnEditorPlayModeStateChanged;
            EditorApplication.pauseStateChanged -= OnEditorPauseStateChanged;
        }

        protected virtual void OnFocus()
        {
            s_LastFocusedEditor = GetInstanceID();

            if (m_Focused)
                return;

            if (rootVisualElement == null)
                return;

            // selection may have changed while Visual Scripting Editor was looking away
            OnGlobalSelectionChange();

            m_Focused = true;
        }

        protected virtual void OnLostFocus()
        {
            m_Focused = false;
        }

        protected virtual void Update()
        {
            if (Store == null)
                return;

            Profiler.BeginSample("GtfoWindow.Update");
            Stopwatch sw = new Stopwatch();
            sw.Start();

            Store.BeginViewUpdate();

            var rebuildType = Store.State.GetUpdateType(m_LastStateVersion);

            UpdateGraphContainer();

            if (m_BlankPage?.panel != null)
                m_BlankPage.UpdateUI();

            if (m_MainToolbar?.panel != null)
                m_MainToolbar?.UpdateUI();

            if (m_ErrorToolbar?.panel != null)
                m_ErrorToolbar?.UpdateUI();

            if (GraphView?.panel != null)
                GraphView.UpdateUI(rebuildType);

            m_LastStateVersion = Store.EndViewUpdate();

            if (Store.State.Preferences.GetBool(BoolPref.WarnOnUIFullRebuild) && rebuildType == UIRebuildType.Complete)
            {
                Debug.LogWarning($"Rebuilding the whole UI ({Store.State.LastDispatchedActionName})");
            }

            if (Store.State.Preferences.GetBool(BoolPref.LogUIBuildTime))
            {
                Debug.Log($"UI Update ({Store.State.LastDispatchedActionName}) took {sw.ElapsedMilliseconds} ms");
            }

            if (Store.State.Preferences.GetBool(BoolPref.AutoRecompile) &&
                m_CompilationTimer.ElapsedMilliseconds >= (EditorApplication.isPlaying
                                                           ? k_IdleTimeBeforeCompilationMsPlayMode
                                                           : k_IdleTimeBeforeCompilationMs))
            {
                m_CompilationTimer.Stop(Store.State.CompilationStateComponent);
                m_CompilationPendingLabel.EnableInClassList(compilationPendingUssClassName, false);

                OnCompilationRequest(RequestCompilationOptions.Default);
            }
        }

        void UndoRedoPerformed()
        {
            if (!RefreshUIDisabled)
                Store.MarkStateDirty();
        }

        void OnEnterPanel(AttachToPanelEvent e)
        {
            Selection.selectionChanged += OnGlobalSelectionChange;
            OnGlobalSelectionChange();
        }

        void OnLeavePanel(DetachFromPanelEvent e)
        {
            // ReSharper disable once DelegateSubtraction
            Selection.selectionChanged -= OnGlobalSelectionChange;
        }
    }
}
