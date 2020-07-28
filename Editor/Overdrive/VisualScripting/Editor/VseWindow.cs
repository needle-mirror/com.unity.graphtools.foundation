#define DEBUG_UI

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.SmartSearch;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.Profiling;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    [PublicAPI]
    public partial class VseWindow : GraphViewEditorWindow, IHasCustomMenu
    {
        public class CompilationTimer
        {
            readonly Stopwatch m_IdleTimer;

            public CompilationTimer()
            {
                m_IdleTimer = new Stopwatch();
            }

            public long ElapsedMilliseconds => m_IdleTimer.IsRunning ? m_IdleTimer.ElapsedMilliseconds : 0;
            public bool IsRunning => m_IdleTimer.IsRunning;

            public void Restart(IGTFEditorDataModel editorDataModel)
            {
                m_IdleTimer.Restart();
                editorDataModel.CompilationPending = true;
            }

            public void Stop(IGTFEditorDataModel editorDataModel)
            {
                m_IdleTimer.Stop();
                editorDataModel.CompilationPending = false;
            }
        }

        public void OnToggleTracing(ChangeEvent<bool> e)
        {
            DataModel.TracingEnabled = e.newValue;
            if (Store.GetState()?.CurrentGraphModel != null)
                Store.GetState().CurrentGraphModel.Stencil.Debugger.OnToggleTracing(Store.GetState()?.CurrentGraphModel, e.newValue);
            OnCompilationRequest(RequestCompilationOptions.Default);
            Menu.UpdateUI();
        }

        const string k_StyleSheetPath = PackageTransitionHelper.AssetPath + "VisualScripting/Editor/Views/Templates/";

        const string k_DefaultGraphAssetName = "VSGraphAsset.asset";

        static int s_LastFocusedEditor = -1;

        public Preferences Preferences => DataModel.Preferences;
        public bool TracingEnabled;

        [SerializeField]
        GameObject m_BoundObject;
        public GameObject BoundObject => m_BoundObject;

        [SerializeField]
        List<OpenedGraph> m_PreviousGraphModels;
        public List<OpenedGraph> PreviousGraphModels => m_PreviousGraphModels;

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

        public string LastGraphFilePath => m_LastGraphFilePath;

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

        ShortcutHandler m_ShortcutHandler;

        PluginRepository m_PluginRepository;

        GtfErrorToolbar m_ErrorToolbar;

        bool m_Focused;

        public VseGraphView GraphView => m_GraphView as VseGraphView;

        protected VseMenu Menu => m_Menu as VseMenu;

        public bool RefreshUIDisabled { private get; set; }

        public enum OpenMode { Open, OpenAndFocus }

        protected virtual bool WithWindowedTools => true;

        public override IEnumerable<GraphView> GraphViews
        {
            get { yield return GraphView; }
        }

        public VseWindow()
        {
            s_LastFocusedEditor = GetInstanceID();
            _compilationTimer = new CompilationTimer();
        }

        public virtual void SetBoundObject(GameObject boundObject)
        {
            m_BoundObject = boundObject;
        }

        public virtual void OnCompilationRequest(RequestCompilationOptions options)
        {
            var compilationOptions = GetCompilationOptions();

            // Register
            var graphModel = Store.GetState().CurrentGraphModel;

            if (graphModel == null || graphModel.Stencil == null)
                return;

            var plugins = graphModel.Stencil.GetCompilationPluginHandlers(compilationOptions);
            m_PluginRepository.RegisterPlugins(plugins);

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

        CompilationOptions GetCompilationOptions()
        {
            CompilationOptions compilationOptions = EditorApplication.isPlaying
                ? CompilationOptions.LiveEditing
                : CompilationOptions.Default;

            if (TracingEnabled)
                compilationOptions |= CompilationOptions.Tracing;
            return compilationOptions;
        }

        public override void UnloadGraph()
        {
            GraphView.UIController.ResetBlackboard();
            GraphView.UIController.Clear();
            base.UnloadGraph();
            Menu.UpdateUI();
        }

        public void UpdateGraphIfAssetMoved()
        {
            var graphModel = Store.GetState()?.CurrentGraphModel;
            if (graphModel != null)
            {
                string assetPath = graphModel.GetAssetPath();
                m_LastGraphFilePath = assetPath;
            }
        }

        protected virtual VseBlankPage CreateBlankPage()
        {
            return new VseBlankPage(Store);
        }

        protected virtual VseMenu CreateMenu()
        {
            return new VseMenu(Store, GraphView){OnToggleTracing = OnToggleTracing};
        }

        protected virtual GtfErrorToolbar CreateErrorToolbar()
        {
            return new GtfErrorToolbar(Store, GraphView);
        }

        protected virtual VseGraphView CreateGraphView()
        {
            return new VseGraphView(this, Store);
        }

        protected virtual void OnEnable()
        {
            if (m_PreviousGraphModels == null)
                m_PreviousGraphModels = new List<OpenedGraph>();

            if (m_BlackboardExpandedRowStates == null)
                m_BlackboardExpandedRowStates = new List<string>();

            if (m_ElementModelsToSelectUponCreation == null)
                m_ElementModelsToSelectUponCreation = new List<string>();

            rootVisualElement.RegisterCallback<ValidateCommandEvent>(OnValidateCommand);
            rootVisualElement.RegisterCallback<ExecuteCommandEvent>(OnExecuteCommand);
            rootVisualElement.RegisterCallback<MouseMoveEvent>(_ =>
            {
                if (_compilationTimer.IsRunning)
                    _compilationTimer.Restart(Store.GetState().EditorDataModel);
            });

            rootVisualElement.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(k_StyleSheetPath + "VSEditor.uss"));

            rootVisualElement.Clear();
            rootVisualElement.style.overflow = Overflow.Hidden;
            rootVisualElement.pickingMode = PickingMode.Ignore;
            rootVisualElement.style.flexDirection = FlexDirection.Column;
            rootVisualElement.name = "vseRoot";

            // Create the store.
            DataModel = CreateDataModel();
            State initialState = CreateInitialState();
            Store = new Store(initialState, StoreHelper.RegisterReducers);

            m_GraphContainer = new VisualElement { name = "graphContainer" };
            m_GraphView = CreateGraphView();
            m_Menu = CreateMenu();
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

            m_PluginRepository = new PluginRepository(Store, this);

            EditorApplication.playModeStateChanged += OnEditorPlayModeStateChanged;
            EditorApplication.pauseStateChanged += OnEditorPauseStateChanged;

            if (DataModel is VSEditorDataModel vsDataModel)
            {
                vsDataModel.PluginRepository = m_PluginRepository;
                vsDataModel.OnCompilationRequest = OnCompilationRequest;
            }
        }

        protected virtual void SetupWindow()
        {
            AddMenu();
            // AddTracingTimeline();
            AddGraphView();
            AddErrorToolbar();
        }

        protected void AddMenu()
        {
            rootVisualElement.Add(m_Menu);
        }

        void AddErrorToolbar()
        {
            if (m_ErrorToolbar != null)
                GraphView.Add(m_ErrorToolbar);
        }

        protected void AddGraphView()
        {
            rootVisualElement.Add(m_GraphContainer);
        }

        protected virtual Dictionary<Event, ShortcutDelegate> GetShortcutDictionary()
        {
            return new Dictionary<Event, ShortcutDelegate>
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
                  IGTFGraphElementModel[] selectedModels = m_GraphView.Selection
                      .OfType<IGraphElement>()
                      .Select(x => x.Model)
                      .ToArray();

                  // Convert variable -> constant if selection contains at least one item that satisfies conditions
                  IGTFVariableNodeModel[] variableModels = selectedModels.OfType<VariableNodeModel>().Cast<IGTFVariableNodeModel>().ToArray();
                  if (variableModels.Any())
                  {
                      Store.Dispatch(new ConvertVariableNodesToConstantNodesAction(variableModels));
                      return EventPropagation.Stop;
                  }

                  IGTFConstantNodeModel[] constantModels = selectedModels.OfType<IGTFConstantNodeModel>().ToArray();
                  if (constantModels.Any())
                      Store.Dispatch(new ConvertConstantNodesToVariableNodesAction(constantModels));
                  return EventPropagation.Stop;
              }},
                { Event.KeyboardEvent("Q"), () => GraphView.AlignSelection(false) },
                { Event.KeyboardEvent("#Q"), () => GraphView.AlignSelection(true) },
                { Event.KeyboardEvent("`"), () => OnCreateStickyNote(new Rect(m_GraphView.ChangeCoordinatesTo(m_GraphView.contentViewContainer, m_GraphView.WorldToLocal(Event.current.mousePosition)), StickyNote.defaultSize)) },
            };
        }

        EventPropagation OnBackspaceKeyDown()
        {
            return GraphView.RemoveSelection();
        }

        EventPropagation RenameElement()
        {
            var renamableSelection = m_GraphView.Selection.OfType<GraphElement>().Where(x => x.IsRenamable()).ToList();

            var lastSelectedItem = renamableSelection.LastOrDefault() as ISelectableGraphElement;

            if (!(lastSelectedItem is IRenamable renamable))
                return EventPropagation.Stop;

            if (renamableSelection.Count > 1)
            {
                m_GraphView.ClearSelection();
                m_GraphView.AddToSelection(lastSelectedItem);
            }

            if (renamable.IsFramable())
                GraphView.FrameSelectionIfNotVisible();
            renamable.Rename(forceRename: true);

            return EventPropagation.Stop;
        }

        void OnEditorPlayModeStateChanged(PlayModeStateChange playMode)
        {
            Menu.UpdateUI();
        }

        void OnEditorPauseStateChanged(PauseState pauseState)
        {
            Menu.UpdateUI();
        }

        void OnSearchInGraph()
        {
            Vector3 pos = m_GraphView.viewTransform.position;
            Vector3 scale = m_GraphView.viewTransform.scale;

            SearcherService.FindInGraph(this, Store.GetState().CurrentGraphModel,
                // when highlighting an entry
                item => GraphView.UIController.UpdateViewTransform(item),
                // when pressing enter/double clicking on an entry
                i =>
                {
                    if (i == null) // cancelled by pressing escape, no selection, reset view
                        GraphView.UpdateViewTransform(pos, scale);
                });
        }

        EventPropagation OnCreateStickyNote(Rect atPosition)
        {
            Store.Dispatch(new CreateStickyNoteAction("Note", atPosition));
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

            Store.Dispose();

            GraphView?.UIController?.ClearCompilationErrors();

            m_PluginRepository?.Dispose();

            EditorApplication.playModeStateChanged -= OnEditorPlayModeStateChanged;
            EditorApplication.pauseStateChanged -= OnEditorPauseStateChanged;
        }

        public void OnCompilationDone(IGTFGraphModel vsGraphModel, CompilationOptions options, CompilationResult results)
        {
            if (!this)
            {
                // Should not happen, but it did, so...
                Debug.LogWarning("A destroyed VseWindow still has an OnCompilationDone callback registered.");
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

        public virtual void AddItemsToMenu(GenericMenu menu)
        {
            var disabled = Store?.GetState().CurrentGraphModel == null;

            m_LockTracker.AddItemsToMenu(menu, disabled);
        }

        protected virtual void ShowButton(Rect atPosition)
        {
            var disabled = Store?.GetState().CurrentGraphModel == null;

            m_LockTracker?.ShowButton(atPosition, disabled);
        }

        const int k_IdleTimeBeforeCompilationSeconds = 1;
        const int k_IdleTimeBeforeCompilationSecondsPlayMode = 1;
        const string k_CompilationPendingClassName = "compilationPending";

        void StoreOnStateChanged()
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
                    _compilationTimer.Restart(editorDataModel);
                    m_CompilationPendingLabel.EnableInClassList(k_CompilationPendingClassName, true);
                    // Register
                    var stencil = Store.GetState().CurrentGraphModel?.Stencil;
                    if (stencil != null)
                    {
                        var plugins = stencil.GetCompilationPluginHandlers(GetCompilationOptions());
                        m_PluginRepository.RegisterPlugins(plugins);
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
                Menu.UpdateUI();
            }

            if (currentUpdateFlags.HasFlag(UpdateFlags.GraphTopology))
            {
                if (graphModel != null)
                {
                    if (!currentUpdateFlags.HasFlag(UpdateFlags.CompilationResult))
                    {
                        _compilationTimer.Restart(editorDataModel);
                        m_CompilationPendingLabel.EnableInClassList(k_CompilationPendingClassName, true);

                        var stencil = Store.GetState().CurrentGraphModel?.Stencil;
                        stencil?.OnCompilationStarted(graphModel);
                    }

                    GraphView.NotifyTopologyChange(graphModel);
                }

                // A topology change should update everything.
                GraphView.UIController.UpdateTopology();
                currentUpdateFlags |= UpdateFlags.All;
            }

            if (currentUpdateFlags.HasFlag(UpdateFlags.CompilationResult))
            {
                UpdateCompilationErrorsDisplay(Store.GetState());
            }

            Store?.GetState()?.CurrentGraphModel?.CheckIntegrity(Verbosity.Errors);

            if (graphModel != null && currentUpdateFlags.HasFlag(UpdateFlags.RequestRebuild))
            {
//                var editors = Resources.FindObjectsOfTypeAll<VisualBehaviourInspector>();
//                foreach (var editor in editors)
//                {
//                    editor.Repaint();
//                }
            }

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

        void UpdateCompilationErrorsDisplay(Overdrive.State state)
        {
            GraphView.UIController.ClearCompilationErrors();
            GraphView.UIController.DisplayCompilationErrors(state);
            m_ErrorToolbar.Update();
        }

        void UndoRedoPerformed()
        {
            Profiler.BeginSample("VseWindow_UndoRedoPerformed");
            if (!RefreshUIDisabled)
                Store.ForceRefreshUI(UpdateFlags.All);
            Profiler.EndSample();
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

        protected virtual void Update()
        {
            if (Store == null)
                return;

            Store.Update();

            if (Preferences.GetBool(BoolPref.AutoRecompile) &&
                _compilationTimer.ElapsedMilliseconds >= (EditorApplication.isPlaying
                                                          ? k_IdleTimeBeforeCompilationSecondsPlayMode
                                                          : k_IdleTimeBeforeCompilationSeconds) * 1000)
            {
                _compilationTimer.Stop(DataModel);
                m_CompilationPendingLabel.EnableInClassList(k_CompilationPendingClassName, false);

                OnCompilationRequest(RequestCompilationOptions.Default);
            }
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

            var selectedToken = m_GraphView.Selection?.OfType<Token>().ToList();
            if (selectedToken?.Any() == true)
            {
                var latestSelectedToken = selectedToken.LastOrDefault();
                var portModel = (latestSelectedToken?.NodeModel as NodeModel)?.OutputsByDisplayOrder[0];
                if (portModel == null)
                {
                    return EventPropagation.Continue;
                }

                Vector2 pos = Event.current.mousePosition;
                SearcherService.ShowOutputToGraphNodes(Store.GetState(), portModel, pos, item =>
                {
                    Store.Dispatch(new CreateNodeFromOutputPortAction(portModel, pos, item));
                });
            }
            else if (GraphView.lastHoveredVisualElement != null)
            {
                var customSearcherHandler = GraphView.lastHoveredVisualElement?.GetFirstOfType<ICustomSearcherHandler>();
                if (customSearcherHandler != null && customSearcherHandler.HandleCustomSearcher(Event.current.mousePosition))
                {
                    return EventPropagation.Continue;
                }

                // TODO this is bad: need GV refactor to introduce an interface for the types we want to whitelist
                if (!(GraphView.lastHoveredVisualElement is Node node))
                {
                    // In case of lastHoveredVisualElement is Label or Port
                    node = GraphView.lastHoveredVisualElement.GetFirstOfType<Node>();
                }

                if (node != null)
                {
                    DisplaySmartSearch();
                    return EventPropagation.Continue;
                }

                var tokenDeclaration = GraphView.lastHoveredVisualElement.GetFirstOfType<TokenDeclaration>();
                if (tokenDeclaration != null)
                {
                    (m_GraphView as VseGraphView)?.DisplayTokenDeclarationSearcher((VariableDeclarationModel)tokenDeclaration.Declaration, Event.current.mousePosition);
                }
                else
                {
                    DisplaySmartSearch();
                }
            }

            return EventPropagation.Continue;
        }

        internal void DisplaySmartSearch(DropdownMenuAction menuAction = null, int insertIndex = -1)
        {
            var lastHoveredElement = GraphView.lastHoveredSmartSearchCompatibleElement;
            Vector2 mousePosition = menuAction?.eventInfo?.mousePosition ?? Event.current.mousePosition;
            Vector2 graphPosition = m_GraphView.contentViewContainer.WorldToLocal(mousePosition);
            switch (lastHoveredElement)
            {
                case Edge edge:
                    SearcherService.ShowEdgeNodes(Store.GetState(), edge.EdgeModel, mousePosition, item =>
                    {
                        Store.Dispatch(new CreateNodeOnEdgeAction(edge.EdgeModel, graphPosition, item));
                    });
                    break;

                default:
                    SearcherService.ShowGraphNodes(Store.GetState(), mousePosition, item =>
                    {
                        Store.Dispatch(new CreateNodeFromSearcherAction(graphPosition, item, new[] {GUID.Generate() }));
                    });
                    break;
            }
        }

        public void RefreshUI(UpdateFlags updateFlags)
        {
            Store.ForceRefreshUI(updateFlags);
        }

        string GetCurrentAssetPath()
        {
            var asset = (GraphAssetModel)Store.GetState().AssetModel;
            return asset == null ? null : AssetDatabase.GetAssetPath(asset);
        }

        GraphElements.Node m_ElementShownInSidePanel;
        Unity.Properties.UI.PropertyElement m_SidePanelPropertyElement;
        readonly CompilationTimer _compilationTimer;

        public void ShowNodeInSidePanel(ISelectableGraphElement selectable, bool show)
        {
            if (!(selectable is GraphElements.Node node) ||
                !((selectable as IGraphElement).Model is IGTFNodeModel nodeModel) || !show)
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
    }
}
