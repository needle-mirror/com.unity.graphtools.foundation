using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.Profiling;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class GraphViewEditorWindow : EditorWindow, IHasCustomMenu
    {
        public static T FindOrCreateGraphWindow<T>() where T : GraphViewEditorWindow
        {
            var myGraphWindow = Resources.FindObjectsOfTypeAll(typeof(T)).OfType<T>().FirstOrDefault();
            if (myGraphWindow == null)
            {
                myGraphWindow = CreateInstance<T>();
            }

            myGraphWindow.Show();
            myGraphWindow.Focus();

            return myGraphWindow;
        }

        const int k_IdleTimeBeforeGraphProcessingMs = 1000;
        const int k_IdleTimeBeforeGraphProcessingMsPlayMode = 1000;

        public static readonly string graphProcessingPendingUssClassName = "graph-processing-pending";

        static int s_LastFocusedEditor = -1;

        [SerializeField]
        GUID m_GUID;

        [SerializeField]
        LockTracker m_LockTracker = new LockTracker();

        bool m_Focused;

        protected GraphView m_GraphView;
        protected VisualElement m_GraphContainer;
        protected BlankPage m_BlankPage;
        protected VisualElement m_SidePanel;
        protected Label m_SidePanelTitle;
        protected Label m_GraphProcessingPendingLabel;
        protected MainToolbar m_MainToolbar;
        protected ErrorToolbar m_ErrorToolbar;

        GraphProcessingTimer m_GraphProcessingTimer;

        uint m_LastStateVersion;

        Node m_ElementShownInSidePanel;

        Unity.Properties.UI.PropertyElement m_SidePanelPropertyElement;

        public string EditorToolName => "UnnamedTool";

        public bool WithSidePanel { get; set; } = true;

        public GUID GUID => m_GUID;

        public virtual IEnumerable<GraphView> GraphViews
        {
            get { yield return GraphView; }
        }

        bool Locked => CommandDispatcher?.GraphToolState?.AssetModel != null && m_LockTracker.IsLocked;

        public CommandDispatcher CommandDispatcher { get; private set; }

        public GraphView GraphView => m_GraphView;
        public MainToolbar MainToolbar => m_MainToolbar;

        public PluginRepository PluginRepository { get; private set; }

        protected virtual IEnumerable<Type> GraphProcessingTriggerCommands { get; } = new[]
        {
            typeof(ReorderEdgeCommand),
            typeof(RenameElementCommand),
            typeof(UpdateConstantNodeValueCommand),
            typeof(UpdatePortConstantCommand),
            typeof(UpdateModelPropertyValueCommand),
            typeof(LoadGraphAssetCommand),
            typeof(ChangeVariableTypeCommand),
            typeof(BuildAllEditorCommand),
            typeof(CreateGraphVariableDeclarationCommand),
            typeof(InitializeVariableCommand),
            typeof(UpdateExposedCommand),
            typeof(CreateEdgeCommand),
            typeof(DeleteElementsCommand),
            typeof(ToggleTracingCommand)
        };

        protected GraphViewEditorWindow()
        {
            s_LastFocusedEditor = GetInstanceID();
            m_GraphProcessingTimer = new GraphProcessingTimer();
        }

        protected virtual GraphToolState CreateInitialState()
        {
            var prefs = Preferences.CreatePreferences(EditorToolName);
            return new GraphToolState(GUID, prefs);
        }

        protected virtual void RegisterCommandHandlers()
        {
            CommandDispatcherHelper.RegisterDefaultCommandHandlers(CommandDispatcher);
        }

        protected virtual BlankPage CreateBlankPage()
        {
            return new BlankPage(CommandDispatcher, Enumerable.Empty<OnboardingProvider>());
        }

        protected virtual MainToolbar CreateMainToolbar()
        {
            return new MainToolbar(CommandDispatcher, GraphView);
        }

        protected virtual ErrorToolbar CreateErrorToolbar()
        {
            return new ErrorToolbar(CommandDispatcher, GraphView);
        }

        protected virtual GraphView CreateGraphView()
        {
            return new GraphView(this, CommandDispatcher);
        }

        protected virtual void Reset()
        {
            if (CommandDispatcher?.GraphToolState == null)
                return;

            CommandDispatcher.GraphToolState.WindowState.CurrentGraph = new OpenedGraph(null, null);
        }

        protected virtual void OnEnable()
        {
            // When we open a window (including when we start the Editor), a new GUID is assigned.
            // When a window is opened and there is a domain reload, the GUID stays the same.
            if (m_GUID == default)
            {
                m_GUID = GUID.Generate();
            }

            var initialState = CreateInitialState();
            CommandDispatcher = new CommandDispatcher(initialState);
            RegisterCommandHandlers();

            PluginRepository = new PluginRepository(this);

            rootVisualElement.Clear();
            rootVisualElement.pickingMode = PickingMode.Ignore;

            m_GraphContainer = new VisualElement { name = "graphContainer" };
            m_GraphView = CreateGraphView();
            m_MainToolbar = CreateMainToolbar();
            m_ErrorToolbar = CreateErrorToolbar();
            m_BlankPage = CreateBlankPage();
            m_BlankPage?.CreateUI();

            if (m_MainToolbar != null)
                rootVisualElement.Add(m_MainToolbar);
            // AddTracingTimeline();
            rootVisualElement.Add(m_GraphContainer);
            if (m_ErrorToolbar != null)
                m_GraphView.Add(m_ErrorToolbar);

            m_GraphContainer.Add(m_GraphView);

            rootVisualElement.name = "gtfRoot";
            rootVisualElement.AddStylesheet("GraphViewWindow.uss");

            // PF FIXME: Use EditorApplication.playModeStateChanged / AssemblyReloadEvents ? Make sure it works on all domain reloads.

            // After a domain reload, all loaded objects will get reloaded and their OnEnable() called again
            // It looks like all loaded objects are put in a deserialization/OnEnable() queue
            // the previous graph's nodes/edges/... might be queued AFTER this window's OnEnable
            // so relying on objects to be loaded/initialized is not safe
            // hence, we need to defer the loading command
            rootVisualElement.schedule.Execute(() =>
            {
                var lastGraphFilePath = CommandDispatcher.GraphToolState.WindowState.LastOpenedGraph.GraphAssetModelPath;
                if (!string.IsNullOrEmpty(lastGraphFilePath))
                {
                    try
                    {
                        CommandDispatcher.Dispatch(new LoadGraphAssetCommand(
                            lastGraphFilePath,
                            CommandDispatcher.GraphToolState.WindowState.LastOpenedGraph.BoundObject,
                            LoadGraphAssetCommand.Type.KeepHistory));
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
                }
                else
                {
                    // Force display of blank page.
                    CommandDispatcher.MarkStateDirty();
                }
            }).ExecuteLater(0);

            CommandDispatcher.RegisterObserver(GraphProcessingObserver, asPostCommandObserver: true);

            rootVisualElement.RegisterCallback<MouseMoveEvent>(_ =>
            {
                if (m_GraphProcessingTimer.IsRunning)
                    m_GraphProcessingTimer.Restart(CommandDispatcher.GraphToolState.GraphProcessingStateComponent);
            });

            m_GraphProcessingPendingLabel = new Label("Graph Processing Pending"){name = "graph-processing-pending-label"};

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
                        CommandDispatcher.Dispatch(new UpdateModelPropertyValueCommand(m_ElementShownInSidePanel.Model, path, m_SidePanelPropertyElement.GetValue<object>(path)));
                        nodeTarget2.Target = element.GetTarget<object>();
                    }
                    else
                        CommandDispatcher.Dispatch(new UpdateModelPropertyValueCommand(m_ElementShownInSidePanel.Model, path, m_SidePanelPropertyElement.GetValue<object>(path)));
                };
                m_SidePanel.Add(m_SidePanelPropertyElement);
                ShowNodeInSidePanel(null, false);
            }

            if (m_SidePanel != null)
                m_GraphContainer.Add(m_SidePanel);

            Undo.undoRedoPerformed += UndoRedoPerformed;

            rootVisualElement.RegisterCallback<AttachToPanelEvent>(OnEnterPanel);
            rootVisualElement.RegisterCallback<DetachFromPanelEvent>(OnLeavePanel);
            // that will be true when the window is restored during the editor startup, so OnEnterPanel won't be called later
            if (rootVisualElement.panel != null)
                OnEnterPanel(null);

            titleContent = new GUIContent("Graph Tool");

            m_LockTracker.lockStateChanged.AddListener(OnLockStateChanged);

            EditorApplication.playModeStateChanged += OnEditorPlayModeStateChanged;
            EditorApplication.pauseStateChanged += OnEditorPauseStateChanged;
        }

        protected virtual void OnDisable()
        {
            // ReSharper disable once DelegateSubtraction
            Undo.undoRedoPerformed -= UndoRedoPerformed;

            UnloadGraph();

            if (m_ErrorToolbar != null)
                m_GraphView.Remove(m_ErrorToolbar);
            rootVisualElement.Remove(m_GraphContainer);
            if (m_MainToolbar != null)
                rootVisualElement.Remove(m_MainToolbar);

            m_GraphView = null;
            m_MainToolbar = null;
            m_ErrorToolbar = null;
            m_BlankPage = null;

            // Calling Dispose() manually to clean things up now, not at GC time.
            CommandDispatcher.Dispose();
            CommandDispatcher = null;

            PluginRepository?.Dispose();
            PluginRepository = null;

            EditorApplication.playModeStateChanged -= OnEditorPlayModeStateChanged;
            EditorApplication.pauseStateChanged -= OnEditorPauseStateChanged;
        }

        protected virtual void OnDestroy()
        {
            // When window is closed, remove all associated state to avoid cluttering the Library folder.
            PersistedEditorState.RemoveViewState(GUID);
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
            if (CommandDispatcher == null)
                return;

            // PF: FIXME find a better solution for this.
            // Prevent UI update while the mouse is captured.
            // Mouse is typically captured on manipulation that span mouseDown/mouseMove/mouseUp events (like drags).
            // Since UI rebuilding may replace VisualElements by new ones (this is what RebuildAll() does)
            // the manipulation loses the VisualElements it was acting upon.
            //
            // Case when this happens: with autorecompilation, a compilation can finish while we are in a manipulation.
            // Then, badges are added to signal errors. Since these are new models, a RebuildAll() is triggered.
            //
            // A better solution would be to avoid all calls to RebuildAll().
            if (rootVisualElement.panel.GetCapturingElement(PointerId.mousePointerId) != null)
            {
                return;
            }

            Profiler.BeginSample("GtfoWindow.Update");
            Stopwatch sw = new Stopwatch();
            sw.Start();

            CommandDispatcher.BeginViewUpdate();

            var rebuildType = CommandDispatcher.GraphToolState.GetUpdateType(m_LastStateVersion);

            UpdateGraphContainer();

            if (m_BlankPage?.panel != null)
                m_BlankPage.UpdateUI();

            if (m_MainToolbar?.panel != null)
                m_MainToolbar?.UpdateUI();

            if (m_ErrorToolbar?.panel != null)
                m_ErrorToolbar?.UpdateUI();

            if (GraphView?.panel != null)
                GraphView.UpdateUI(rebuildType);

            m_LastStateVersion = CommandDispatcher.EndViewUpdate();

            if (CommandDispatcher.GraphToolState.Preferences.GetBool(BoolPref.WarnOnUIFullRebuild) && rebuildType == UIRebuildType.Complete)
            {
                Debug.LogWarning($"Rebuilding the whole UI ({CommandDispatcher.GraphToolState.LastDispatchedCommandName})");
            }

            if (CommandDispatcher.GraphToolState.Preferences.GetBool(BoolPref.LogUIBuildTime))
            {
                Debug.Log($"UI Update ({CommandDispatcher.GraphToolState.LastDispatchedCommandName}) took {sw.ElapsedMilliseconds} ms");
            }

            if (CommandDispatcher.GraphToolState.Preferences.GetBool(BoolPref.AutoProcess) &&
                m_GraphProcessingTimer.ElapsedMilliseconds >= (EditorApplication.isPlaying
                                                               ? k_IdleTimeBeforeGraphProcessingMsPlayMode
                                                               : k_IdleTimeBeforeGraphProcessingMs))
            {
                m_GraphProcessingTimer.Stop(CommandDispatcher.GraphToolState.GraphProcessingStateComponent);
                m_GraphProcessingPendingLabel.EnableInClassList(graphProcessingPendingUssClassName, false);

                OnGraphProcessingRequest(RequestGraphProcessingOptions.Default);
            }
        }

        public void AdjustWindowMinSize(Vector2 size)
        {
            // Set the window min size from the graphView, adding the menu bar height
            minSize = new Vector2(size.x, size.y + m_MainToolbar?.layout.height ?? 0);
        }

        void OnEditorPlayModeStateChanged(PlayModeStateChange playMode)
        {
            MainToolbar?.UpdateUI();
        }

        void OnEditorPauseStateChanged(PauseState pauseState)
        {
            MainToolbar?.UpdateUI();
        }

        void UndoRedoPerformed()
        {
            CommandDispatcher.MarkStateDirty();
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

        public void ShowNodeInSidePanel(ISelectableGraphElement selectable, bool show)
        {
            if (m_SidePanel == null)
                return;

            if (!(selectable is Node node) ||
                !(selectable.Model is INodeModel nodeModel) || !show)
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
            var disabled = CommandDispatcher.GraphToolState.GraphModel == null;

            m_LockTracker.AddItemsToMenu(menu, disabled);
        }

        public override IEnumerable<Type> GetExtraPaneTypes()
        {
            return Assembly
                .GetAssembly(typeof(GraphViewToolWindow))
                .GetTypes()
                .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(GraphViewToolWindow)));
        }

        public static void ShowGraphViewWindowWithTools<T>() where T : GraphViewEditorWindow
        {
            var windows = GraphViewStaticBridge.ShowGraphViewWindowWithTools(typeof(GraphViewBlackboardWindow), typeof(GraphViewMinimapWindow), typeof(T));
            var graphView = (windows[0] as T)?.GraphViews.FirstOrDefault();
            if (graphView != null)
            {
                (windows[1] as GraphViewBlackboardWindow)?.SelectGraphViewFromWindow((windows[0] as T), graphView);
                (windows[2] as GraphViewMinimapWindow)?.SelectGraphViewFromWindow((windows[0] as T), graphView);
            }
        }

        protected void UpdateGraphContainer()
        {
            var graphModel = CommandDispatcher?.GraphToolState?.GraphModel;

            if (graphModel != null)
            {
                if (m_GraphContainer.Contains(m_BlankPage))
                    m_GraphContainer.Remove(m_BlankPage);
                if (!m_GraphContainer.Contains(m_GraphView))
                    m_GraphContainer.Insert(0, m_GraphView);
                if (!m_GraphContainer.Contains(m_SidePanel))
                    m_GraphContainer.Add(m_SidePanel);
                if (!rootVisualElement.Contains(m_GraphProcessingPendingLabel))
                    rootVisualElement.Add(m_GraphProcessingPendingLabel);
            }
            else
            {
                if (m_GraphContainer.Contains(m_SidePanel))
                    m_GraphContainer.Remove(m_SidePanel);
                if (m_GraphContainer.Contains(m_GraphView))
                    m_GraphContainer.Remove(m_GraphView);
                if (!m_GraphContainer.Contains(m_BlankPage))
                    m_GraphContainer.Insert(0, m_BlankPage);
                if (rootVisualElement.Contains(m_GraphProcessingPendingLabel))
                    rootVisualElement.Remove(m_GraphProcessingPendingLabel);
            }
        }

        public virtual void UnloadGraph()
        {
            CommandDispatcher.GraphToolState.UnloadCurrentGraphAsset();
            PluginRepository?.UnregisterPlugins();
            UpdateGraphContainer();
            GraphView.UnloadGraph();
            MainToolbar?.UpdateUI();
        }

        public void UnloadGraphIfDeleted()
        {
            var iGraphModel = CommandDispatcher.GraphToolState.AssetModel as ScriptableObject;
            if (!iGraphModel)
            {
                UnloadGraph();
            }
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

            foreach (var onboardingProvider in m_BlankPage?.OnboardingProviders ?? Enumerable.Empty<OnboardingProvider>())
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
            var windows = (GraphViewEditorWindow[])Resources.FindObjectsOfTypeAll(typeof(GraphViewEditorWindow));

            // Only the last focused editor should try to answer a change to the current selection.
            if (s_LastFocusedEditor != GetInstanceID() && windows.Length > 1)
                return;

            // PF FIXME load correct asset type (not GraphAssetModel)
            if (AssetDatabase.LoadAssetAtPath(graphAssetFilePath, typeof(GraphAssetModel)))
            {
                var currentOpenedGraph = CommandDispatcher.GraphToolState?.WindowState.CurrentGraph ?? default;
                // don't load if same graph and same bound object
                if (CommandDispatcher.GraphToolState?.AssetModel != null &&
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
            CommandDispatcher.Dispatch(new LoadGraphAssetCommand(graphAssetFilePath, boundObject));
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

        protected virtual bool CanHandleAssetType(GraphAssetModel asset)
        {
            return true;
        }

        string GetCurrentAssetPath()
        {
            var asset = CommandDispatcher.GraphToolState.AssetModel;
            return asset == null ? null : AssetDatabase.GetAssetPath(asset as Object);
        }

        public enum OpenMode { Open, OpenAndFocus }

        void GraphProcessingObserver(Command action)
        {
            if (GraphProcessingTriggerCommands.Contains(action.GetType()))
            {
                ProcessGraph();
            }
        }

        internal void ProcessGraph()
        {
            m_GraphProcessingTimer.Restart(CommandDispatcher.GraphToolState.GraphProcessingStateComponent);
            m_GraphProcessingPendingLabel.EnableInClassList(graphProcessingPendingUssClassName, true);

            // Register
            var stencil = CommandDispatcher.GraphToolState.GraphModel?.Stencil;
            if (stencil != null)
            {
                var plugins = stencil.GetGraphProcessingPluginHandlers(GetGraphProcessingOptions(CommandDispatcher.GraphToolState.TracingState));
                PluginRepository.RegisterPlugins(plugins);
                stencil.OnGraphProcessingStarted(CommandDispatcher.GraphToolState?.GraphModel);
                // PF FIXME does this really trigger a graph processing? Should we call OnGraphProcessingRequest?
            }
        }

        static GraphProcessingOptions GetGraphProcessingOptions(TracingStateComponent tracingState)
        {
            GraphProcessingOptions graphProcessingOptions = EditorApplication.isPlaying
                ? GraphProcessingOptions.LiveEditing
                : GraphProcessingOptions.Default;

            if (tracingState.TracingEnabled)
                graphProcessingOptions |= GraphProcessingOptions.Tracing;
            return graphProcessingOptions;
        }

        void OnGraphProcessingRequest(RequestGraphProcessingOptions options)
        {
            var graphProcessingOptions = GetGraphProcessingOptions(CommandDispatcher.GraphToolState.TracingState);

            // Register
            var graphModel = CommandDispatcher.GraphToolState.GraphModel;

            if (graphModel?.Stencil == null)
                return;

            var plugins = graphModel.Stencil.GetGraphProcessingPluginHandlers(graphProcessingOptions);
            PluginRepository.RegisterPlugins(plugins);

            var graphProcessor = graphModel.Stencil.CreateGraphProcessor();
            if (options == RequestGraphProcessingOptions.SaveGraph)
                AssetDatabase.SaveAssets();

            var r = graphProcessor.ProcessGraph(graphModel);
            if (CommandDispatcher?.GraphToolState != null)
            {
                CommandDispatcher.GraphToolState.GraphProcessingStateComponent.m_LastResult = r;
                OnGraphProcessingDone(CommandDispatcher.GraphToolState);
            }
        }

        static void OnGraphProcessingDone(GraphToolState state)
        {
            ConsoleWindowBridge.RemoveLogEntries();

            var deletedBadges = state.GraphModel.DeleteBadgesOfType<GraphProcessingErrorBadgeModel>();
            var newBadges = new List<IGraphElementModel>();

            var graphProcessingResult = state.GraphProcessingStateComponent.GetLastResult();
            if (graphProcessingResult?.Errors != null && graphProcessingResult.Errors.Count > 0)
            {
                var graphAsset = state.AssetModel;
                foreach (var error in graphProcessingResult.Errors)
                {
                    if (error.SourceNode != null && !error.SourceNode.Destroyed)
                    {
                        var badgeModel = new GraphProcessingErrorBadgeModel(error);
                        state.GraphModel.AddBadge(badgeModel);
                        newBadges.Add(badgeModel);
                    }

                    if (graphAsset != null && graphAsset is Object asset)
                    {
                        var graphAssetPath = asset ? AssetDatabase.GetAssetPath(asset) : "<unknown>";
                        ConsoleWindowBridge.LogSticky(
                            $"{graphAssetPath}: {error.Description}",
                            $"{graphAssetPath}@{error.SourceNodeGuid}",
                            error.IsWarning ? LogType.Warning : LogType.Error,
                            LogOption.None,
                            asset.GetInstanceID());
                    }
                }
            }

            if (deletedBadges.Count > 0 || newBadges.Count > 0)
            {
                state.MarkDeleted(deletedBadges);
                state.MarkNew(newBadges);
            }
        }
    }
}
