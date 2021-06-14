using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.Profiling;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Base class for windows to edit graphs.
    /// </summary>
    public abstract class GraphViewEditorWindow : EditorWindow, IHasCustomMenu
    {
        /// <summary>
        /// Finds the first window of type <typeparamref name="WindowT"/>. If no window is found, create a new one.
        /// The window is then opened and focused.
        /// </summary>
        /// <typeparam name="WindowT">The window type, which should derive from <see cref="GraphViewEditorWindow"/>.</typeparam>
        /// <returns>A window.</returns>
        public static WindowT FindOrCreateGraphWindow<WindowT>() where WindowT : GraphViewEditorWindow
        {
            var window = Resources.FindObjectsOfTypeAll(typeof(WindowT)).OfType<WindowT>().FirstOrDefault();
            if (window == null)
            {
                window = CreateInstance<WindowT>();
            }

            window.Show();
            window.Focus();

            return window;
        }

        /// <summary>
        /// Ways of opening a graph when selection changes.
        /// </summary>
        public enum OpenMode
        {
            /// <summary>
            /// Just open the graph in the window.
            /// </summary>
            Open,

            /// <summary>
            /// Show the graph and focus the window.
            /// </summary>
            OpenAndFocus
        }

        public static readonly string graphProcessingPendingUssClassName = "graph-processing-pending";

        static int s_LastFocusedEditor = -1;

        [SerializeField]
        SerializableGUID m_GUID;

        [SerializeField]
        LockTracker m_LockTracker = new LockTracker();

        bool m_Focused;

        protected GraphView m_GraphView;
        protected VisualElement m_GraphContainer;
        protected BlankPage m_BlankPage;
        protected ModelInspectorView m_SidePanel;
        protected Label m_GraphProcessingPendingLabel;
        protected MainToolbar m_MainToolbar;
        protected ErrorToolbar m_ErrorToolbar;

        AutomaticGraphProcessor m_AutomaticGraphProcessor;
        GraphProcessingStatusObserver m_GraphProcessingStatusObserver;
        SidePanelObserver m_SidePanelObserver;

        public string EditorToolName
        {
            get;
            protected set;
        } = "UnnamedTool";

        public bool WithSidePanel { get; set; } = true;

        public SerializableGUID GUID => m_GUID;

        public virtual IEnumerable<GraphView> GraphViews
        {
            get { yield return GraphView; }
        }

        bool Locked => m_LockTracker.IsLocked;

        public CommandDispatcher CommandDispatcher { get; private set; }

        public GraphView GraphView => m_GraphView;
        public MainToolbar MainToolbar => m_MainToolbar;

        public PluginRepository PluginRepository { get; private set; }

        static GraphViewEditorWindow()
        {
            SetupLogStickyCallback();
        }

        protected GraphViewEditorWindow()
        {
            s_LastFocusedEditor = GetInstanceID();
        }

        protected virtual GraphToolState CreateInitialState()
        {
            var prefs = Preferences.CreatePreferences(EditorToolName);
            return new GraphToolState(GUID, prefs);
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
            return new GraphView(this, CommandDispatcher, EditorToolName);
        }

        protected virtual ModelInspectorView CreateModelInspectorView()
        {
            return new ModelInspectorView(CommandDispatcher);
        }

        protected virtual void Reset()
        {
            CommandDispatcher?.State?.LoadGraphAsset(null, null);
        }

        protected virtual void OnEnable()
        {
            // When we open a window (including when we start the Editor), a new GUID is assigned.
            // When a window is opened and there is a domain reload, the GUID stays the same.
            if (m_GUID == default)
            {
                m_GUID = SerializableGUID.Generate();
            }

            var initialState = CreateInitialState();
            CommandDispatcher = new CommandDispatcher(initialState);

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
                var lastGraphFilePath = CommandDispatcher.State.WindowState.LastOpenedGraph.GetGraphAssetModelPath();
                var lastGraphId = CommandDispatcher.State.WindowState.LastOpenedGraph.AssetLocalId;
                if (!string.IsNullOrEmpty(lastGraphFilePath))
                {
                    try
                    {
                        CommandDispatcher.Dispatch(new LoadGraphAssetCommand(
                            lastGraphFilePath,
                            lastGraphId,
                            PluginRepository,
                            CommandDispatcher.State.WindowState.LastOpenedGraph.BoundObject,
                            LoadGraphAssetCommand.LoadStrategies.KeepHistory));
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
                }
            }).ExecuteLater(0);

            m_GraphProcessingPendingLabel = new Label("Graph Processing Pending") { name = "graph-processing-pending-label" };

            if (WithSidePanel)
            {
                m_SidePanel = CreateModelInspectorView();
            }

            if (m_SidePanel != null)
                m_GraphContainer.Add(m_SidePanel);

            rootVisualElement.RegisterCallback<AttachToPanelEvent>(OnEnterPanel);
            rootVisualElement.RegisterCallback<DetachFromPanelEvent>(OnLeavePanel);
            // that will be true when the window is restored during the editor startup, so OnEnterPanel won't be called later
            if (rootVisualElement.panel != null)
                OnEnterPanel(null);

            titleContent = new GUIContent("Graph Tool");

            m_LockTracker.lockStateChanged.AddListener(OnLockStateChanged);

            m_AutomaticGraphProcessor = new AutomaticGraphProcessor(PluginRepository);
            CommandDispatcher.RegisterObserver(m_AutomaticGraphProcessor);
            rootVisualElement.RegisterCallback<MouseMoveEvent>(ResetGraphProcessorTimer);

            m_GraphProcessingStatusObserver = new GraphProcessingStatusObserver(m_GraphProcessingPendingLabel, m_ErrorToolbar);
            CommandDispatcher.RegisterObserver(m_GraphProcessingStatusObserver);

            m_SidePanelObserver = new SidePanelObserver(this);
            CommandDispatcher.RegisterObserver(m_SidePanelObserver);
        }

        protected virtual void OnDisable()
        {
            m_AutomaticGraphProcessor?.StopTimer();
            CommandDispatcher.UnregisterObserver(m_AutomaticGraphProcessor);
            CommandDispatcher.UnregisterObserver(m_GraphProcessingStatusObserver);
            CommandDispatcher.UnregisterObserver(m_SidePanelObserver);

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
        }

        protected virtual void OnDestroy()
        {
            // When window is closed, remove all associated state to avoid cluttering the Library folder.
            PersistedState.RemoveViewState(GUID);
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

            m_GraphView?.Focus();
        }

        protected virtual void OnLostFocus()
        {
            m_Focused = false;
        }

        protected virtual void Update()
        {
            if (CommandDispatcher == null)
                return;

            Profiler.BeginSample("GraphViewEditorWindow.Update");
            Stopwatch sw = new Stopwatch();
            sw.Start();

            // PF FIXME To StateObserver, eventually
            UpdateGraphContainer();

            CommandDispatcher.NotifyObservers();

            sw.Stop();

            if (CommandDispatcher.State.Preferences.GetBool(BoolPref.LogUIBuildTime))
            {
                Debug.Log($"UI Update ({CommandDispatcher.LastDispatchedCommandName}) took {sw.ElapsedMilliseconds} ms");
            }

            UpdateDirtyState(GraphView?.GraphModel?.AssetModel?.Dirty ?? false);

            Profiler.EndSample();
        }

        public void AdjustWindowMinSize(Vector2 size)
        {
            // Set the window min size from the graphView, adding the menu bar height
            minSize = new Vector2(size.x, size.y + m_MainToolbar?.layout.height ?? 0);
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

        protected void ResetGraphProcessorTimer(MouseMoveEvent e)
        {
            if (CommandDispatcher.State.Preferences.GetBool(BoolPref.AutoProcess))
            {
                m_AutomaticGraphProcessor.ResetTimer();
            }
        }

        public virtual void AddItemsToMenu(GenericMenu menu)
        {
            var disabled = CommandDispatcher.State.WindowState.GraphModel == null;

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
            var graphModel = CommandDispatcher?.State?.WindowState.GraphModel;

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
            CommandDispatcher.State.LoadGraphAsset(null, null);
            PluginRepository?.UnregisterPlugins();
            UpdateGraphContainer();
            GraphView.UnloadGraph();
        }

        public void UnloadGraphIfDeleted()
        {
            var iGraphModel = CommandDispatcher.State.WindowState.AssetModel as ScriptableObject;
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
                if (onboardingProvider.GetGraphAndObjectFromSelection(CommandDispatcher, Selection.activeObject, out var graphAssetModel, out var boundObject))
                {
                    SetCurrentSelection(graphAssetModel, OpenMode.Open, boundObject);
                    return;
                }
            }

            if (Selection.activeObject is IGraphAssetModel graph && CanHandleAssetType(graph))
            {
                SetCurrentSelection(graph, OpenMode.Open);
            }
        }

        public void SetCurrentSelection(IGraphAssetModel graphAssetModel, OpenMode mode, GameObject boundObject = null)
        {
            var windows = (GraphViewEditorWindow[])Resources.FindObjectsOfTypeAll(typeof(GraphViewEditorWindow));

            // Only the last focused editor should try to answer a change to the current selection.
            if (s_LastFocusedEditor != GetInstanceID() && windows.Length > 1)
                return;

            var currentOpenedGraph = CommandDispatcher?.State?.WindowState.CurrentGraph ?? default;
            // don't load if same graph and same bound object
            if (CommandDispatcher?.State?.WindowState.AssetModel != null &&
                graphAssetModel == currentOpenedGraph.GetGraphAssetModel() &&
                currentOpenedGraph.BoundObject == boundObject)
                return;

            var graphAssetFilePath = graphAssetModel.GetPath();
            var fileId = graphAssetModel.GetFileId();
            // If there is not graph asset, unload the current one.
            if (string.IsNullOrWhiteSpace(graphAssetFilePath) || fileId == 0)
            {
                return;
            }

            // Load this graph asset.
            CommandDispatcher?.Dispatch(new LoadGraphAssetCommand(graphAssetFilePath, fileId, PluginRepository, boundObject));

            if (GraphView?.GraphModel?.AssetModel != null)
                UpdateDirtyState(GraphView.GraphModel.AssetModel.Dirty);

            if (mode != OpenMode.OpenAndFocus)
                return;

            // Check if an existing window already has this asset, if yes give it the focus.
            foreach (var window in windows)
            {
                if (window.CommandDispatcher.State.WindowState.AssetModel == graphAssetModel)
                {
                    window.Focus();
                    return;
                }
            }
        }

        /// <summary>
        /// Indicates if the graphview window can handle the given <paramref name="asset"/>.
        /// </summary>
        /// <param name="asset">The asset we want to know if hte window handles</param>
        /// <returns>True if the window can handle the givne <paramref name="asset"/>. False otherwise.</returns>
        protected abstract bool CanHandleAssetType(IGraphAssetModel asset);

        static void SetupLogStickyCallback()
        {
            ConsoleWindowBridge.SetEntryDoubleClickedDelegate((file, entryInstanceId) =>
            {
                // FIXME: will not work with files that contains multiple GraphAssetModel.

                var pathAndGuid = file.Split('@');

                bool assetOpened = false;
                var asset = AssetDatabase.LoadAssetAtPath<GraphAssetModel>(pathAndGuid[0]);
                if (asset != null)
                    assetOpened = AssetDatabase.OpenAsset(asset);

                if (assetOpened)
                {
                    var guid = new SerializableGUID(pathAndGuid[1]);
                    var window = focusedWindow as GraphViewEditorWindow;
                    if (window != null && window.GraphView.GraphModel.TryGetModelFromGuid(guid, out var nodeModel))
                    {
                        var graphElement = nodeModel.GetUI<GraphElement>(window.GraphView);
                        if (graphElement != null)
                        {
                            window.GraphView.DispatchFrameAndSelectElementsCommand(true, graphElement);
                        }
                    }
                }
            });
        }

        void UpdateDirtyState(bool dirty)
        {
            titleContent = new GUIContent(EditorToolName + (dirty ? "*" : ""));
        }

    }
}
