#define DEBUG_UI

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor.Callbacks;
using UnityEditor.EditorCommon;
using UnityEditor.EditorCommon.Extensions;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ProjectWindowCallback;
using UnityEditor.VisualScripting.Editor.Plugins;
using UnityEditor.VisualScripting.Editor.Renamable;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEditor.VisualScripting.Model.Translators;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;
using UnityEngine.VisualScripting;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace UnityEditor.VisualScripting.Editor
{
    [PublicAPI]
    public partial class VseWindow : GraphViewEditorWindow, IHasCustomMenu
    {
        [MenuItem("Window/Visual Script", false, 2020)]
        static void ShowNewVsEditorWindow()
        {
            ShowVsEditorWindow();
        }

        public void OnToggleTracing(ChangeEvent<bool> e)
        {
            DataModel.TracingEnabled = e.newValue;
            OnCompilationRequest(RequestCompilationOptions.Default);
            Menu.UpdateUI();
        }

        const string k_StyleSheetPath = PackageTransitionHelper.AssetPath + "VisualScripting/Editor/Resources/";

        const string k_DefaultGraphAssetName = "VSGraphAsset.asset";

        static int s_LastFocusedEditor = -1;

        public VSPreferences Preferences => DataModel.Preferences;
        public bool TracingEnabled;


        public GameObject BoundObject => null; // TODO: m_BoundObject;

        string m_LastGraphFilePath;

        [SerializeField]
        List<GraphModel> m_PreviousGraphModels;
        public List<GraphModel> PreviousGraphModels => m_PreviousGraphModels;

        // TODO: Until serialization/persistent data is brought back into VisualElements, we need
        // a place for keeping otherwise-non serializable data, like blackboard related data (expanded/selected states, size, etc.)
        // Note that all this data is indirectly used via the Editor Data Model and should someday have its own
        // local implementation (e.g. directly in the Blackboard)
        [SerializeField]
        List<string> m_BlackboardExpandedRowStates;
        public List<string> BlackboardExpandedRowStates => m_BlackboardExpandedRowStates;

        [SerializeField]
        List<string> m_ElementModelsToSelectUponCreation;
        public List<string> ElementModelsToSelectUponCreation => m_ElementModelsToSelectUponCreation;

        [SerializeField]
        List<string> m_ElementModelsToExpandUponCreation;
        public List<string> ElementModelsToExpandUponCreation => m_ElementModelsToExpandUponCreation;

        string LastGraphFilePath => m_LastGraphFilePath;

        public IEditorDataModel DataModel { get; private set; }

        protected virtual IEditorDataModel CreateDataModel()
        {
            return new VSEditorDataModel(this);
        }

        protected virtual State CreateInitialState()
        {
            return new State(DataModel);
        }

        // Window itself

        VseGraphView m_GraphView;

        ShortcutHandler m_ShortcutHandler;

        PluginRepository m_PluginRepository;

        Store m_Store;

        VseMenu m_Menu;

        VseBlankPage m_BlankPage;

        VisualElement m_GraphContainer;

        SourceCodePhases m_CodeViewPhase = SourceCodePhases.Initial;

        TracingTimeline m_TracingTimeline;

        public SourceCodePhases ToggleCodeViewPhase
        {
            get
            {
                var current = m_CodeViewPhase;
                m_CodeViewPhase++;
                if (m_CodeViewPhase > SourceCodePhases.Final)
                    m_CodeViewPhase = SourceCodePhases.Initial;
                return current;
            }
        }

        bool m_Focused;

        public VseGraphView GraphView => m_GraphView;

        public Store Store => m_Store;

        protected VseMenu Menu => m_Menu;

        public IGraphModel CurrentGraphModel => m_Store.GetState()?.CurrentGraphModel;

        public bool RefreshUIDisabled { private get; set; }

        public static VseWindow ShowVsEditorWindow()
        {
            //getting all the VseWindows except derived classes
            var vseWindows = Resources.FindObjectsOfTypeAll(typeof(VseWindow)).OfExactType<VseWindow>().ToArray();
            var window = vseWindows.Length > 0 ? vseWindows[0] : CreateInstance<VseWindow>();
            window.Show();
            window.Focus();
            return window;
        }

        protected enum OpenMode { Open, OpenAndFocus }

        // Double-click an asset to load it and show it in the VSE -- step 1
        [OnOpenAsset(1)]
        public static bool OpenVseAsset(int instanceId, int line)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceId);
            if (obj is VSGraphAssetModel)
            {
                string path = AssetDatabase.GetAssetPath(instanceId);
                return OpenVseAssetInWindow(path) != null;
            }

            return false;
        }

        public static VseWindow OpenVseAssetInWindow(string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<VSGraphAssetModel>(path);
            if (asset == null)
                return null;

            VseWindow vseWindow = ShowVsEditorWindow();

            vseWindow.SetCurrentSelection(asset, OpenMode.OpenAndFocus);

            return vseWindow;
        }

        protected virtual bool WithWindowedTools => true;

        public override IEnumerable<GraphView> graphViews
        {
            get { yield return GraphView; }
        }

        public VseWindow()
        {
            s_LastFocusedEditor = GetInstanceID();
        }

        public virtual void SetBoundObject(GameObject boundObject)
        {
            throw new NotImplementedException("SetBoundObject");
        }

        public virtual void OnCompilationRequest(RequestCompilationOptions options)
        {
            CompilationOptions compilationOptions = EditorApplication.isPlaying
                ? CompilationOptions.LiveEditing
                : CompilationOptions.Default;

            if (TracingEnabled)
                compilationOptions |= CompilationOptions.Tracing;

            // Register
            m_PluginRepository.RegisterPlugins(compilationOptions);

            VSGraphModel vsGraphModel = Store.GetState().CurrentGraphModel as VSGraphModel;
            if (vsGraphModel == null || vsGraphModel.Stencil == null)
                return;

            ITranslator translator = vsGraphModel.Stencil.CreateTranslator();
            if (!translator.SupportsCompilation())
                return;

            if (options == RequestCompilationOptions.SaveGraph)
                AssetDatabase.SaveAssets();

            CompilationResult r = vsGraphModel.Compile(AssemblyType.None, translator, compilationOptions, m_PluginRepository.GetPluginHandlers());
            if (Store?.GetState()?.CompilationResultModel is CompilationResultModel compilationResultModel) // TODO: could have disappeared during the await
            {
                compilationResultModel.lastResult = r;
                OnCompilationDone(vsGraphModel, compilationOptions, r);
            }
        }

        public void UnloadGraphIfDeleted()
        {
            var iGraphModel = m_Store.GetState().CurrentGraphModel as ScriptableObject;
            if (!iGraphModel)
            {
                m_GraphView.UIController.ResetBlackboard();
                m_GraphView.UIController.Clear();
                m_Store.GetState().UnloadCurrentGraphAsset();
                m_LastGraphFilePath = null;
                Menu.UpdateUI();
                UpdateGraphContainer();
            }
        }

        public void UpdateGraphIfAssetMoved()
        {
            IGraphModel graphModel = m_Store.GetState()?.CurrentGraphModel;
            if (graphModel != null)
            {
                string assetPath = graphModel.GetAssetPath();
                m_LastGraphFilePath = assetPath;
            }
        }

        protected virtual VseBlankPage CreateBlankPage()
        {
            return new VseBlankPage(m_Store);
        }

        protected virtual VseMenu CreateMenu()
        {
            return new VseMenu(m_Store, m_GraphView){OnToggleTracing = OnToggleTracing};
        }

        protected virtual VseGraphView CreateGraphView()
        {
            return new VseGraphView(this, m_Store);
        }

        protected virtual void OnEnable()
        {
            if (m_PreviousGraphModels == null)
                m_PreviousGraphModels = new List<GraphModel>();

            if (m_BlackboardExpandedRowStates == null)
                m_BlackboardExpandedRowStates = new List<string>();

            if (m_ElementModelsToSelectUponCreation == null)
                m_ElementModelsToSelectUponCreation = new List<string>();

            if (m_ElementModelsToExpandUponCreation == null)
                m_ElementModelsToExpandUponCreation = new List<string>();

            rootVisualElement.RegisterCallback<ValidateCommandEvent>(OnValidateCommand);
            rootVisualElement.RegisterCallback<ExecuteCommandEvent>(OnExecuteCommand);
            rootVisualElement.RegisterCallback<MouseMoveEvent>(_ => m_IdleTimer?.Restart());

            rootVisualElement.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(k_StyleSheetPath + "VSEditor.uss"));

            rootVisualElement.Clear();
            rootVisualElement.style.overflow = Overflow.Hidden;
            rootVisualElement.pickingMode = PickingMode.Ignore;
            rootVisualElement.style.flexDirection = FlexDirection.Column;
            rootVisualElement.name = "vseRoot";

            // Create the store.
            DataModel = CreateDataModel();
            State initialState = CreateInitialState();
            m_Store = new Store(initialState, Store.Options.TrackUndoRedo);

            VseUtility.SetupLogStickyCallback();

            m_GraphContainer = new VisualElement { name = "graphContainer" };
            m_GraphView = CreateGraphView();
            m_Menu = CreateMenu();
            m_BlankPage = CreateBlankPage();


            IMGUIContainer imguiContainer = null;
            imguiContainer = new IMGUIContainer(() =>
            {
                var timeRect = new Rect(0, 0, rootVisualElement.layout.width, imguiContainer.layout.height);
                m_TracingTimeline.OnGUI(timeRect);
            });
            m_TracingTimeline = new TracingTimeline(m_GraphView, m_Store.GetState(), imguiContainer);
            m_TracingTimeline.SyncVisible();

            rootVisualElement.Add(m_Menu);
            rootVisualElement.Add(imguiContainer);
            rootVisualElement.Add(m_GraphContainer);

            m_CompilationPendingLabel = new Label("Compilation Pending"){name = "compilationPendingLabel"};

            m_GraphContainer.Add(m_GraphView);

            m_ShortcutHandler = new ShortcutHandler(
                new Dictionary<Event, ShortcutDelegate>
                {
                    { Event.KeyboardEvent("F2"), () => Application.platform != RuntimePlatform.OSXEditor ? RenameElement() : EventPropagation.Continue },
                    { Event.KeyboardEvent("F5"), () =>
                  {
                      RefreshUI(UpdateFlags.All);
                      return EventPropagation.Continue;
                  }},
                    { Event.KeyboardEvent("return"), () => Application.platform == RuntimePlatform.OSXEditor ? RenameElement() : EventPropagation.Continue },
                    { Event.KeyboardEvent("[enter]"), () => Application.platform == RuntimePlatform.OSXEditor ? RenameElement() : EventPropagation.Continue },
                    { Event.KeyboardEvent("backspace"), OnBackspaceKeyDown },
                    { Event.KeyboardEvent("space"), OnSpaceKeyDown },
                    { Event.KeyboardEvent("C"), () =>
                  {
                      IGraphElementModel[] selectedModels = m_GraphView.selection
                          .OfType<IHasGraphElementModel>()
                          .Select(x => x.GraphElementModel)
                          .ToArray();

                      // Convert variable -> constant if selection contains at least one item that satisfies conditions
                      IVariableModel[] variableModels = selectedModels.OfType<VariableNodeModel>().Cast<IVariableModel>().ToArray();
                      if (variableModels.Any())
                      {
                          m_Store.Dispatch(new ConvertVariableNodesToConstantNodesAction(variableModels));
                          return EventPropagation.Stop;
                      }

                      IConstantNodeModel[] constantModels = selectedModels.OfType<IConstantNodeModel>().ToArray();
                      if (constantModels.Any())
                          m_Store.Dispatch(new ConvertConstantNodesToVariableNodesAction(constantModels));
                      return EventPropagation.Stop;
                  }},
                    { Event.KeyboardEvent("Q"), () => m_GraphView.AlignSelection(false) },
                    { Event.KeyboardEvent("#Q"), () => m_GraphView.AlignSelection(true) },
                    // DEBUG
                    { Event.KeyboardEvent("1"), () => OnCreateLogNode(LogNodeModel.LogTypes.Message) },
                    { Event.KeyboardEvent("2"), () => OnCreateLogNode(LogNodeModel.LogTypes.Warning) },
                    { Event.KeyboardEvent("3"), () => OnCreateLogNode(LogNodeModel.LogTypes.Error) },
                    { Event.KeyboardEvent("`"), () => OnCreateStickyNote(new Rect(m_GraphView.ChangeCoordinatesTo(m_GraphView.contentViewContainer, m_GraphView.WorldToLocal(Event.current.mousePosition)), StickyNote.defaultSize)) },
                });

            rootVisualElement.parent.AddManipulator(m_ShortcutHandler);
            Selection.selectionChanged += OnGlobalSelectionChange;

            m_Store.StateChanged += StoreOnStateChanged;
            Undo.undoRedoPerformed += UndoRedoPerformed;

            rootVisualElement.RegisterCallback<AttachToPanelEvent>(OnEnterPanel);
            rootVisualElement.RegisterCallback<DetachFromPanelEvent>(OnLeavePanel);

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
                        m_Store.Dispatch(new LoadGraphAssetAction(LastGraphFilePath, loadType: LoadGraphAssetAction.Type.KeepHistory));
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
                }
                else             // will display the blank page. not needed otherwise as the LoadGraphAsset reducer will refresh
                    m_Store.Dispatch(new RefreshUIAction(UpdateFlags.All));
            }).ExecuteLater(0);


            m_LockTracker.lockStateChanged.AddListener(OnLockStateChanged);

            m_PluginRepository = new PluginRepository(m_Store, m_GraphView);

            EditorApplication.playModeStateChanged += OnEditorPlayModeStateChanged;
            EditorApplication.pauseStateChanged += OnEditorPauseStateChanged;

            if (DataModel is VSEditorDataModel vsDataModel)
            {
                vsDataModel.PluginRepository = m_PluginRepository;
                vsDataModel.OnCompilationRequest = OnCompilationRequest;
            }
        }

        EventPropagation OnBackspaceKeyDown()
        {
            return GraphView.RemoveSelection();
        }

        EventPropagation RenameElement()
        {
            var renamableSelection = m_GraphView.selection.OfType<GraphElement>().Where(x => x.IsRenamable()).ToList();

            var lastSelectedItem = renamableSelection.LastOrDefault() as ISelectable;

            if (!(lastSelectedItem is IRenamable renamable))
                return EventPropagation.Stop;

            if (renamableSelection.Count > 1)
            {
                m_GraphView.ClearSelection();
                m_GraphView.AddToSelection(lastSelectedItem);
            }

            if (renamable.IsFramable())
                m_GraphView.FrameSelectionIfNotVisible();
            renamable.Rename(forceRename: true);

            return EventPropagation.Stop;
        }

        void OnEditorPlayModeStateChanged(PlayModeStateChange playMode)
        {
            m_Menu.UpdateUI();
        }

        void OnEditorPauseStateChanged(PauseState pauseState)
        {
            m_Menu.UpdateUI();
        }

        void OnSearchInGraph()
        {
            Vector3 pos = m_GraphView.viewTransform.position;
            Vector3 scale = m_GraphView.viewTransform.scale;

            SearcherService.FindInGraph(this, (VSGraphModel)m_Store.GetState().CurrentGraphModel,
                // when highlighting an entry
                item => m_GraphView.UIController.UpdateViewTransform(item),
                // when pressing enter/double clicking on an entry
                i =>
                {
                    if (i == null) // cancelled by pressing escape, no selection, reset view
                        m_GraphView.UpdateViewTransform(pos, scale);
                });
        }

        EventPropagation OnCreateLogNode(LogNodeModel.LogTypes logType)
        {
            var stack = m_GraphView.lastHoveredSmartSearchCompatibleElement as StackNode;
            IStackModel stackModel = stack?.stackModel;
            if (stackModel != null)
            {
                m_Store.Dispatch(new CreateLogNodeAction(stackModel, logType));
                return EventPropagation.Stop;
            }

            return EventPropagation.Continue;
        }

        EventPropagation OnCreateStickyNote(Rect atPosition)
        {
            m_Store.Dispatch(new CreateStickyNoteAction("Note", atPosition));
            return EventPropagation.Stop;
        }

        protected void OnDisable()
        {
            // Clear previous compilation errors from the output log.
            // ReSharper disable once DelegateSubtraction
            Undo.undoRedoPerformed -= UndoRedoPerformed;

            if (rootVisualElement != null)
            {
                if (m_ShortcutHandler != null)
                    rootVisualElement.parent.RemoveManipulator(m_ShortcutHandler);

                rootVisualElement.UnregisterCallback<ValidateCommandEvent>(OnValidateCommand);
                rootVisualElement.UnregisterCallback<ExecuteCommandEvent>(OnExecuteCommand);
            }

            m_Store.Dispose();

            m_GraphView?.UIController?.ClearCompilationErrors();

            // ReSharper disable once DelegateSubtraction
            Selection.selectionChanged -= OnGlobalSelectionChange;

            m_PluginRepository?.Dispose();

            EditorApplication.playModeStateChanged -= OnEditorPlayModeStateChanged;
            EditorApplication.pauseStateChanged -= OnEditorPauseStateChanged;
        }

        public void OnCompilationDone(VSGraphModel vsGraphModel, CompilationOptions options, CompilationResult results)
        {
            if (!this)
            {
                // Should not happen, but it did, so...
                Debug.LogWarning("A destroyed VseWindow still has an OnCompilationDone callback registered.");
                return;
            }

            State state = m_Store.GetState();
            VseUtility.UpdateCodeViewer(show: false, sourceIndex: SourceCodePhases.Final,
                compilationResult: results,
                selectionDelegate: lineMetadata =>
                {
                    if (lineMetadata == null)
                        return;

                    GUID nodeGuid = (GUID)lineMetadata;
                    m_Store.Dispatch(new PanToNodeAction(nodeGuid));
                });

            //TODO: incremental re-register
            m_PluginRepository.RegisterPlugins(options);

            UpdateCompilationErrorsDisplay(state);

            if (results != null && results.errors.Count == 0)
            {
                // TODO : Add delegate to register to compilation Done
//                VSCompilationService.NotifyChange((ISourceProvider)vsGraphModel.assetModel);
            }
        }

        public virtual void AddItemsToMenu(GenericMenu menu)
        {
            var disabled = m_Store?.GetState().CurrentGraphModel == null;

            m_LockTracker.AddItemsToMenu(menu, disabled);
        }

        protected virtual void ShowButton(Rect atPosition)
        {
            var disabled = m_Store?.GetState().CurrentGraphModel == null;

            m_LockTracker?.ShowButton(atPosition, disabled);
        }

        Stopwatch m_IdleTimer;
        private Label m_CompilationPendingLabel;

        const int k_IdleTimeBeforeCompilationSeconds = 1;
        const int k_IdleTimeBeforeCompilationSecondsPlayMode = 1;
        const string k_CompilationPendingClassName = "compilationPending";

        void StoreOnStateChanged()
        {
            var editorDataModel = m_Store.GetState().EditorDataModel;

            UpdateFlags currentUpdateFlags = editorDataModel.UpdateFlags;
            if (currentUpdateFlags == 0)
                return;

            IGraphModel graphModel = m_Store.GetState()?.CurrentGraphModel;

            m_LastGraphFilePath = graphModel?.GetAssetPath();

            if (currentUpdateFlags.HasFlag(UpdateFlags.RequestCompilation))
            {
                if (!currentUpdateFlags.HasFlag(UpdateFlags.CompilationResult))
                {
                    m_IdleTimer = Stopwatch.StartNew();
                    m_CompilationPendingLabel.EnableInClassList(k_CompilationPendingClassName, true);
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
                m_Menu.UpdateUI();

                m_GraphView.schedule.Execute(() =>
                {
                    if (editorDataModel.NodeToFrameGuid != default)
                    {
                        m_GraphView.PanToNode(editorDataModel.NodeToFrameGuid);
                        editorDataModel.NodeToFrameGuid = default;
                    }
                }).ExecuteLater(1);
            }

            if (currentUpdateFlags.HasFlag(UpdateFlags.GraphTopology))
            {
                if (graphModel != null)
                {
                    if (!currentUpdateFlags.HasFlag(UpdateFlags.CompilationResult))
                    {
                        m_IdleTimer = Stopwatch.StartNew();
                        m_CompilationPendingLabel.EnableInClassList(k_CompilationPendingClassName, true);
                    }

                    m_GraphView.NotifyTopologyChange(graphModel);
                }

                // A topology change should update everything.
                m_GraphView.UIController.UpdateTopology();
                currentUpdateFlags |= UpdateFlags.All;
            }

            if (currentUpdateFlags.HasFlag(UpdateFlags.CompilationResult))
            {
                UpdateCompilationErrorsDisplay(m_Store.GetState());
            }

            ((VSGraphModel)m_Store.GetState().CurrentGraphModel)?.CheckIntegrity(GraphModel.Verbosity.Errors);

            if (graphModel != null && currentUpdateFlags.HasFlag(UpdateFlags.RequestRebuild))
            {
//                var editors = Resources.FindObjectsOfTypeAll<VisualBehaviourInspector>();
//                foreach (var editor in editors)
//                {
//                    editor.Repaint();
//                }
            }

            if (graphModel != null && graphModel.LastChanges.ModelsToAutoAlign.Any())
            {
                var elementsToAlign = graphModel.LastChanges.ModelsToAutoAlign
                    .Select(n => m_GraphView.UIController.ModelsToNodeMapping[n]);
                m_GraphView.schedule.Execute(() =>
                {
                    m_GraphView.AlignGraphElements(elementsToAlign);

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

        void UpdateCompilationErrorsDisplay(State state)
        {
            m_GraphView.UIController.ClearCompilationErrors();
            m_GraphView.UIController.DisplayCompilationErrors(state);
            m_Menu.UpdateErrorMenu();
        }

        void UpdateGraphContainer()
        {
            IGraphModel graphModel = m_Store.GetState().CurrentGraphModel;

            if (graphModel != null)
            {
                if (m_GraphContainer.Contains(m_BlankPage))
                    m_GraphContainer.Remove(m_BlankPage);
                if (!m_GraphContainer.Contains(m_GraphView))
                    m_GraphContainer.Insert(0, m_GraphView);
                if (!rootVisualElement.Contains(m_CompilationPendingLabel))
                    rootVisualElement.Add(m_CompilationPendingLabel);
            }
            else
            {
                if (m_GraphContainer.Contains(m_GraphView))
                    m_GraphContainer.Remove(m_GraphView);
                if (!m_GraphContainer.Contains(m_BlankPage))
                    m_GraphContainer.Insert(0, m_BlankPage);
                if (rootVisualElement.Contains(m_CompilationPendingLabel))
                    rootVisualElement.Remove(m_CompilationPendingLabel);
            }
        }

        void UndoRedoPerformed()
        {
            Profiler.BeginSample("VseWindow_UndoRedoPerformed");
            if (!RefreshUIDisabled)
                m_Store.Dispatch(new RefreshUIAction(UpdateFlags.All));
            Profiler.EndSample();
        }

        void OnEnterPanel(AttachToPanelEvent e)
        {
            rootVisualElement.parent.AddManipulator(m_ShortcutHandler);
            Selection.selectionChanged += OnGlobalSelectionChange;
        }

        void OnLeavePanel(DetachFromPanelEvent e)
        {
            rootVisualElement.parent.RemoveManipulator(m_ShortcutHandler);
            // ReSharper disable once DelegateSubtraction
            Selection.selectionChanged -= OnGlobalSelectionChange;
        }

        public void AdjustWindowMinSize(Vector2 size)
        {
            // Set the window min size from the graphView, adding the menu bar height
            minSize = new Vector2(size.x, size.y + m_Menu.layout.height);
        }

        static void OnValidateCommand(ValidateCommandEvent evt)
        {
            if (MouseCaptureController.IsMouseCaptured())
                return;
            if (evt.commandName == "Find")
            {
                evt.StopPropagation();
                if (evt.imguiEvent == null)
                    return;
                evt.imguiEvent.Use();
            }
        }

        void OnExecuteCommand(ExecuteCommandEvent evt)
        {
            if (MouseCaptureController.IsMouseCaptured())
                return;
            if (evt.commandName == "Find")
            {
                OnSearchInGraph();
                evt.StopPropagation();
            }
        }

        void OnFocus()
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

        void OnLostFocus()
        {
            m_Focused = false;
        }

        void Update()
        {
            if (m_Store == null)
                return;

            m_Store.GetState().EditorDataModel.UpdateCounter++;
            m_Store.Update();

            IGraphModel currentGraphModel = m_Store.GetState().CurrentGraphModel;
            Stencil currentStencil = currentGraphModel?.Stencil;
            bool stencilRecompilationRequested = currentStencil != null && currentStencil.RecompilationRequested;

            if (stencilRecompilationRequested ||
                m_IdleTimer != null && Preferences.GetBool(VSPreferences.BoolPref.AutoRecompile) &&
                m_IdleTimer.ElapsedMilliseconds >= (EditorApplication.isPlaying ? k_IdleTimeBeforeCompilationSecondsPlayMode : k_IdleTimeBeforeCompilationSeconds) * 1000)
            {
                if (currentStencil != null && stencilRecompilationRequested)
                    currentStencil.RecompilationRequested = false;

                m_IdleTimer?.Stop();
                m_CompilationPendingLabel.EnableInClassList(k_CompilationPendingClassName, false);

                m_IdleTimer = null;
                OnCompilationRequest(RequestCompilationOptions.Default);
            }

            m_TracingTimeline.SyncVisible();

            if (EditorApplication.isPlaying && !EditorApplication.isPaused)
            {
                m_TracingTimeline.Dirty = false;
                m_Store.GetState().currentTracingFrame = Time.frameCount;
                m_Menu.UpdateUI();
            }

            if (m_TracingTimeline.Dirty)
            {
                m_TracingTimeline.Dirty = false;
                m_Menu.UpdateUI();
            }
        }

        public static void CreateGraphAsset<TStencilType>(string graphAssetName = k_DefaultGraphAssetName, IGraphTemplate template = null)
            where TStencilType : Stencil
        {
            string uniqueFilePath = VseUtility.GetUniqueAssetPathNameInActiveFolder(graphAssetName);
            string modelName = Path.GetFileName(uniqueFilePath);

            var endAction = CreateInstance<DoCreateVisualScriptAsset>();
            endAction.Template = template;
            endAction.StencilType = typeof(TStencilType);
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, endAction, modelName, GetIcon(), null);
        }

        public static void CreateGraphAsset<TStencilType, TAssetType>(string graphAssetName = k_DefaultGraphAssetName)
            where TStencilType : Stencil
            where TAssetType : GraphAssetModel
        {
            string uniqueFilePath = VseUtility.GetUniqueAssetPathNameInActiveFolder(graphAssetName);
            string modelName = Path.GetFileName(uniqueFilePath);

            var endAction = CreateInstance<DoCreateVisualScriptAsset>();
            endAction.StencilType = typeof(TStencilType);
            endAction.AssetType = typeof(TAssetType);
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, endAction, modelName, GetIcon(), null);
        }

        public static void CreateGraphAsset<TStencilType, TAssetType, TGraphType>(string graphAssetName = k_DefaultGraphAssetName)
            where TStencilType : Stencil
            where TAssetType : GraphAssetModel
            where TGraphType : GraphModel
        {
            string uniqueFilePath = VseUtility.GetUniqueAssetPathNameInActiveFolder(graphAssetName);
            string modelName = Path.GetFileName(uniqueFilePath);

            var endAction = CreateInstance<DoCreateVisualScriptAsset>();
            endAction.StencilType = typeof(TStencilType);
            endAction.AssetType = typeof(TAssetType);
            endAction.GraphType = typeof(TGraphType);
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, endAction, modelName, GetIcon(), null);
        }

        public static Texture2D GetIcon()
        {
            // TODO: Eventually (when VisualScripting becomes known in trunk):
            // return EditorGUIUtility.IconContent("vs Script Icon").image as Texture2D;
            // This will also remove the hardcoded 64x64 icon usage here (16/32/64 icons are available)
            return Resources.Load("visual_script_component@5x", typeof(Texture2D)) as Texture2D;
        }

        public void AddVisualScriptToObject(int objectInstanceId, string assetPath)
        {
            throw new NotImplementedException();

//            var gameObject = EditorUtility.InstanceIDToObject(objectInstanceId) as GameObject;
//            SetCurrentSelection(gameObject, OpenMode.Open);
        }

        EventPropagation OnSpaceKeyDown()
        {
            return DisplayAppropriateSearcher();
        }

        public EventPropagation DisplayAppropriateSearcher()
        {
            if (m_GraphView.panel == null)
                return EventPropagation.Continue;

            var selectedToken = m_GraphView.selection?.OfType<Token>().ToList();
            if (selectedToken?.Any() == true)
            {
                var latestSelectedToken = selectedToken.LastOrDefault();
                if (!(latestSelectedToken?.output.userData is IPortModel portModel))
                {
                    return EventPropagation.Continue;
                }

                Vector2 pos = Event.current.mousePosition;
                SearcherService.ShowOutputToGraphNodes(m_Store.GetState(), portModel, pos, item =>
                {
                    m_Store.Dispatch(new CreateNodeFromOutputPortAction(portModel, pos, item));
                });
            }
            else if (m_GraphView.lastHoveredVisualElement != null)
            {
                var addButton = m_GraphView.lastHoveredVisualElement.GetFirstOfType<Button>();
                if (addButton != null && addButton.name == FunctionNode.AddButtonName)
                {
                    DisplayFunctionVariableSearcher(addButton, Event.current.mousePosition);
                    return EventPropagation.Continue;
                }

                var customSearcherHandler = m_GraphView.lastHoveredVisualElement?.GetFirstOfType<ICustomSearcherHandler>();
                if (customSearcherHandler != null && customSearcherHandler.HandleCustomSearcher(Event.current.mousePosition))
                {
                    return EventPropagation.Continue;
                }

                // TODO this is bad: need GV refactor to introduce an interface for the types we want to whitelist
                if (!(m_GraphView.lastHoveredVisualElement is Node node))
                {
                    // In case of lastHoveredVisualElement is Label or Port
                    node = m_GraphView.lastHoveredVisualElement.GetFirstOfType<Node>();
                }

                if (node != null)
                {
                    // TODO: that's terrible
                    if (node.GraphElementModel is PropertyGroupBaseNodeModel model)
                    {
                        DisplayPropertySearcher(model, Event.current.mousePosition);
                    }
                    else
                    {
                        DisplaySmartSearch();
                    }

                    return EventPropagation.Continue;
                }

                var tokenDeclaration = m_GraphView.lastHoveredVisualElement.GetFirstOfType<TokenDeclaration>();
                if (tokenDeclaration != null)
                {
                    DisplayTokenDeclarationSearcher((VariableDeclarationModel)tokenDeclaration.Declaration, Event.current.mousePosition);
                }
                else
                {
                    DisplaySmartSearch();
                }
            }

            return EventPropagation.Continue;
        }

        internal void DisplayAddVariableSearcher(Vector2 pos)
        {
            if (m_Store.GetState().CurrentGraphModel == null)
            {
                return;
            }

            SearcherService.ShowTypes(
                m_Store.GetState().CurrentGraphModel.Stencil,
                pos,
                (t, i) =>
                {
                    Focus();

                    VSGraphModel graphModel = (VSGraphModel)m_Store.GetState().CurrentGraphModel;
                    VariableDeclarationModel declaration = graphModel.CreateGraphVariableDeclaration(
                        "newVariable",
                        t,
                        true
                    );
                    DataModel.ElementModelToRename = declaration;
                    RefreshUI(UpdateFlags.All);
                });
        }

        internal static void DisplayFunctionVariableSearcher(Button addButton, Vector2 pos)
        {
            var functionNode = addButton.GetFirstOfType<FunctionNode>();
            if (functionNode == null)
            {
                return;
            }

            Stencil stencil = functionNode.Store.GetState().CurrentGraphModel.Stencil;
            SearcherService.ShowTypes(stencil, pos,
                (t, i) =>
                {
                    functionNode.CreateFunctionField(t, FunctionNode.SupportedFields.Variable);
                });
        }

        internal void DisplayTokenDeclarationSearcher(VariableDeclarationModel declaration, Vector2 pos)
        {
            if (m_Store.GetState().CurrentGraphModel == null || !declaration.Capabilities.HasFlag(CapabilityFlags.Modifiable))
            {
                return;
            }

            SearcherService.ShowTypes(
                m_Store.GetState().CurrentGraphModel.Stencil,
                pos,
                (t, i) =>
                {
                    var graphModel = (VSGraphModel)m_Store.GetState().CurrentGraphModel;
                    declaration.DataType = t;

                    foreach (var usage in graphModel.FindUsages(declaration))
                    {
                        usage.UpdateTypeFromDeclaration();
                    }

                    RefreshUI(UpdateFlags.All);
                });
        }

        internal void DisplayPropertySearcher(PropertyGroupBaseNodeModel model, Vector2 displayAtPosition)
        {
            var items = PropertyGroupSearcherAdapter.GetPropertySearcherItems(model, 4)?.ToList();
            if (items == null)
                return;

            var adapter = new PropertyGroupSearcherAdapter(m_Store, model);
            SearcherService.ShowTransientData(this, items, adapter,
                item =>
                {
                    if (item == null)
                        return;

                    PropertyElement propertyElement = ((PropertySearcherItem)item).Element;
                    adapter.EditModel(!propertyElement.Toggle.value, propertyElement.Item);
                },
                displayAtPosition);
        }

        internal void DisplaySmartSearch(DropdownMenuAction menuAction = null, int insertIndex = -1)
        {
            Vector2 mouseWorldPos = menuAction?.eventInfo.mousePosition ?? Event.current.mousePosition;

            if (insertIndex == -1)
            {
                if (m_GraphView.lastHoveredVisualElement is Node node && node.IsInStack)
                {
                    insertIndex = node.FindIndexInStack();
                    if (node.Stack.HasBranchedNode() && insertIndex > node.Stack.stackModel.NodeModels.Count() - 1)
                        insertIndex = node.Stack.stackModel.NodeModels.Count() - 1;
                }
                else
                {
                    if (m_GraphView.lastHoveredVisualElement is StackNode stack && stack.HasBranchedNode())
                    {
                        insertIndex = Math.Max(0, stack.stackModel.NodeModels.Count() - 1);
                    }
                    else
                    {
                        stack = m_GraphView.lastHoveredVisualElement.GetFirstAncestorOfType<StackNode>();

                        if (stack != null)
                        {
                            // In all likelihood, we're hovering a stack separator
                            insertIndex = stack.GetInsertionIndex(mouseWorldPos);
                        }
                    }
                }
            }

            var lastHoveredElement = m_GraphView.lastHoveredSmartSearchCompatibleElement;
            Vector2 mousePosition = menuAction?.eventInfo?.mousePosition ?? Event.current.mousePosition;
            Vector2 graphPosition = m_GraphView.contentViewContainer.WorldToLocal(mousePosition);
            switch (lastHoveredElement)
            {
                case StackNode stackNode:
                    SearcherService.ShowStackNodes(m_Store.GetState(), stackNode.stackModel, mousePosition, item =>
                    {
                        m_Store.Dispatch(new CreateStackedNodeFromSearcherAction(
                            stackNode.stackModel, insertIndex, item));
                    });
                    break;

                // Do not prompt searcher if it's a loop edge
                case Edge edge when !(edge.model.OutputPortModel.NodeModel is LoopNodeModel)
                    || !(edge.model.InputPortModel.NodeModel is LoopStackModel):
                    SearcherService.ShowEdgeNodes(m_Store.GetState(), edge.model, mousePosition, item =>
                    {
                        m_Store.Dispatch(new CreateNodeOnEdgeAction(edge.model, graphPosition, item));
                    });
                    break;

                default:
                    SearcherService.ShowGraphNodes(m_Store.GetState(), mousePosition, item =>
                    {
                        m_Store.Dispatch(new CreateNodeFromSearcherAction(
                            m_Store.GetState().CurrentGraphModel, graphPosition, item));
                    });
                    break;
            }
        }

        public void RefreshUI(UpdateFlags updateFlags)
        {
            m_Store.Dispatch(new RefreshUIAction(updateFlags));
        }

        string GetCurrentAssetPath()
        {
            var asset = (GraphAssetModel)m_Store.GetState().AssetModel;
            return asset == null ? null : AssetDatabase.GetAssetPath(asset);
        }
    }

    class DoCreateVisualScriptAsset : EndNameEditAction
    {
        public Type StencilType { private get; set; }
        public Type GraphType
        {
            private get => m_GraphType ?? typeof(VSGraphModel);
            set => m_GraphType = value;
        }

        Type m_AssetType;
        Type m_GraphType;
        public IGraphTemplate Template;

        public Type AssetType
        {
            private get => m_AssetType ?? typeof(VSGraphAssetModel);
            set => m_AssetType = value;
        }

        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            var modelName = Path.GetFileNameWithoutExtension(pathName);

            var initialState = new State(null);
            var store = new Store(initialState);
            store.Dispatch(new CreateGraphAssetAction(StencilType, GraphType, AssetType, modelName, pathName, graphTemplate: Template));
            ProjectWindowUtil.ShowCreatedAsset(store.GetState().AssetModel as Object);
            store.Dispose();
        }
    }
}
