using System;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A toolbar to display new/save/minimap/blackboard buttons and other action buttons and information.
    /// </summary>
    public class MainToolbar : Toolbar
    {
        class UpdateObserver : StateObserver<GraphToolState>
        {
            MainToolbar m_Toolbar;

            public UpdateObserver(MainToolbar toolbar)
                : base(nameof(GraphToolState.WindowState))
            {
                m_Toolbar = toolbar;
            }

            protected override void Observe(GraphToolState state)
            {
                if (m_Toolbar?.panel != null)
                {
                    using (var observation = this.ObserveState(state.WindowState))
                    {
                        if (observation.UpdateType != UpdateType.None)
                        {
                            m_Toolbar.UpdateCommonMenu();
                            m_Toolbar.UpdateBreadcrumbMenu(state);
                        }
                    }
                }
            }
        }

        public new static readonly string ussClassName = "ge-main-toolbar";

        UpdateObserver m_UpdateObserver;

        protected ToolbarBreadcrumbs m_Breadcrumb;
        protected ToolbarButton m_NewGraphButton;
        protected ToolbarButton m_SaveAllButton;
        protected ToolbarButton m_BuildAllButton;
        protected ToolbarButton m_ShowMiniMapButton;
        protected ToolbarButton m_ShowBlackboardButton;
        protected ToolbarToggle m_EnableTracingButton;
        protected ToolbarButton m_OptionsButton;

        public static readonly string NewGraphButton = "newGraphButton";
        public static readonly string SaveAllButton = "saveAllButton";
        public static readonly string BuildAllButton = "buildAllButton";
        public static readonly string ShowMiniMapButton = "showMiniMapButton";
        public static readonly string ShowBlackboardButton = "showBlackboardButton";
        public static readonly string EnableTracingButton = "enableTracingButton";
        public static readonly string OptionsButton = "optionsButton";

        /// <summary>
        /// Initializes a new instance of the <see cref="MainToolbar"/> class.
        /// </summary>
        /// <param name="commandDispatcher">The command dispatcher.</param>
        /// <param name="graphView">The graph view to which to attach the toolbar.</param>
        public MainToolbar(CommandDispatcher commandDispatcher, GraphView graphView) : base(commandDispatcher, graphView)
        {
            AddToClassList(ussClassName);

            this.AddStylesheet("MainToolbar.uss");
            this.AddStylesheet(EditorGUIUtility.isProSkin ? "MainToolbar_dark.uss" : "MainToolbar_light.uss");

            var tpl = GraphElementHelper.LoadUXML("MainToolbar.uxml");
            tpl.CloneTree(this);

            m_NewGraphButton = this.MandatoryQ<ToolbarButton>(NewGraphButton);
            m_NewGraphButton.tooltip = "New Graph";
            m_NewGraphButton.ChangeClickEvent(OnNewGraphButton);

            m_SaveAllButton = this.MandatoryQ<ToolbarButton>(SaveAllButton);
            m_SaveAllButton.tooltip = "Save All";
            m_SaveAllButton.ChangeClickEvent(OnSaveAllButton);

            m_BuildAllButton = this.MandatoryQ<ToolbarButton>(BuildAllButton);
            m_BuildAllButton.tooltip = "Build All";
            m_BuildAllButton.ChangeClickEvent(OnBuildAllButton);

            m_ShowMiniMapButton = this.MandatoryQ<ToolbarButton>(ShowMiniMapButton);
            m_ShowMiniMapButton.tooltip = "Show MiniMap";
            m_ShowMiniMapButton.ChangeClickEvent(ShowGraphViewToolWindow<GraphViewMinimapWindow>);

            m_ShowBlackboardButton = this.MandatoryQ<ToolbarButton>(ShowBlackboardButton);
            m_ShowBlackboardButton.tooltip = "Show Blackboard";
            m_ShowBlackboardButton.ChangeClickEvent(ShowGraphViewToolWindow<GraphViewBlackboardWindow>);

            m_Breadcrumb = this.MandatoryQ<ToolbarBreadcrumbs>("breadcrumb");

            m_EnableTracingButton = this.MandatoryQ<ToolbarToggle>(EnableTracingButton);
            m_EnableTracingButton.tooltip = "Toggle Tracing For Current Instance";
            m_EnableTracingButton.RegisterValueChangedCallback(e => m_CommandDispatcher.Dispatch(new ActivateTracingCommand(e.newValue)));

            m_OptionsButton = this.MandatoryQ<ToolbarButton>(OptionsButton);
            m_OptionsButton.tooltip = "Options";
            m_OptionsButton.ChangeClickEvent(OnOptionsButton);

            RegisterCallback<AttachToPanelEvent>(OnEnterPanel);
            RegisterCallback<DetachFromPanelEvent>(OnLeavePanel);
        }

        /// <summary>
        /// AttachToPanelEvent event callback.
        /// </summary>
        /// <param name="e">The event.</param>
        protected virtual void OnEnterPanel(AttachToPanelEvent e)
        {
            if (m_UpdateObserver == null)
                m_UpdateObserver = new UpdateObserver(this);
            m_CommandDispatcher?.RegisterObserver(m_UpdateObserver);
        }

        /// <summary>
        /// DetachFromPanelEvent event callback.
        /// </summary>
        /// <param name="e">The event.</param>
        protected virtual void OnLeavePanel(DetachFromPanelEvent e)
        {
            m_CommandDispatcher?.UnregisterObserver(m_UpdateObserver);
        }

        void UpdateBreadcrumbMenu(GraphToolState state)
        {
            bool isEnabled = state.WindowState.GraphModel != null;
            if (!isEnabled)
            {
                m_Breadcrumb.style.display = DisplayStyle.None;
                return;
            }
            m_Breadcrumb.style.display = StyleKeyword.Null;

            var i = 0;
            var graphModels = state.WindowState.SubGraphStack;
            for (; i < graphModels.Count; i++)
            {
                var label = GetBreadcrumbLabel(state, i);
                m_Breadcrumb.CreateOrUpdateItem(i, label, BreadcrumbClickedEvent);
            }

            var newCurrentGraph = GetBreadcrumbLabel(state, -1);
            if (newCurrentGraph != null)
            {
                m_Breadcrumb.CreateOrUpdateItem(i, newCurrentGraph, BreadcrumbClickedEvent);
                i++;
            }

            m_Breadcrumb.TrimItems(i);
        }

        protected virtual string GetBreadcrumbLabel(GraphToolState state, int index)
        {
            var graphModels = state.WindowState.SubGraphStack;
            string graphName = null;
            if (index == -1)
            {
                graphName = state.WindowState.CurrentGraph.GetGraphAssetModel()?.FriendlyScriptName;
            }
            else if (index >= 0 && index < graphModels.Count)
            {
                graphName = graphModels[index].GetGraphAssetModel()?.FriendlyScriptName;
            }

            return string.IsNullOrEmpty(graphName) ? "<Unknown>" : graphName;
        }

        protected void BreadcrumbClickedEvent(int i)
        {
            var state = m_CommandDispatcher.State;
            OpenedGraph graphToLoad = default;
            var graphModels = state.WindowState.SubGraphStack;
            if (i < graphModels.Count)
                graphToLoad = graphModels[i];

            OnBreadcrumbClick(graphToLoad, i);
        }

        /// <summary>
        /// Callback for when the user clicks on a breadcrumb element.
        /// </summary>
        /// <param name="graphToLoad">The graph to load.</param>
        /// <param name="breadcrumbIndex">The index of the breadcrumb element clicked.</param>
        protected virtual void OnBreadcrumbClick(OpenedGraph graphToLoad, int breadcrumbIndex)
        {
            if (graphToLoad.GetGraphAssetModel()?.FriendlyScriptName != null)
                m_CommandDispatcher.Dispatch(new LoadGraphAssetCommand(graphToLoad.GetGraphAssetModelPath(), graphToLoad.AssetLocalId, m_GraphView.Window.PluginRepository,
                    graphToLoad.BoundObject, LoadGraphAssetCommand.LoadStrategies.KeepHistory, breadcrumbIndex));
        }

        void ShowGraphViewToolWindow<T>() where T : GraphViewToolWindow
        {
            var existingToolWindow = ConsoleWindowBridge.FindBoundGraphViewToolWindow<T>(m_GraphView);
            if (existingToolWindow == null)
                ConsoleWindowBridge.SpawnAttachedViewToolWindow<T>(m_GraphView.Window, m_GraphView);
            else
                existingToolWindow.Focus();
        }

        /// <summary>
        /// Updates the state of the toolbar common buttons.
        /// </summary>
        protected virtual void UpdateCommonMenu()
        {
            bool enabled = m_CommandDispatcher.State.WindowState.GraphModel != null;

            m_NewGraphButton.SetEnabled(enabled);
            m_SaveAllButton.SetEnabled(enabled);

            var stencil = (Stencil)m_CommandDispatcher.State?.WindowState.GraphModel?.Stencil;
            var toolbarProvider = stencil?.GetToolbarProvider();

            if (!(toolbarProvider?.ShowButton(NewGraphButton) ?? true))
            {
                m_NewGraphButton.style.display = DisplayStyle.None;
            }
            else
            {
                m_NewGraphButton.style.display = StyleKeyword.Null;
            }

            if (!(toolbarProvider?.ShowButton(SaveAllButton) ?? true))
            {
                m_SaveAllButton.style.display = DisplayStyle.None;
            }
            else
            {
                m_SaveAllButton.style.display = StyleKeyword.Null;
            }

            if (!(toolbarProvider?.ShowButton(BuildAllButton) ?? false))
            {
                m_BuildAllButton.style.display = DisplayStyle.None;
            }
            else
            {
                m_BuildAllButton.style.display = StyleKeyword.Null;
            }

            if (!(toolbarProvider?.ShowButton(ShowMiniMapButton) ?? false))
            {
                m_ShowMiniMapButton.style.display = DisplayStyle.None;
            }
            else
            {
                m_ShowMiniMapButton.style.display = StyleKeyword.Null;
            }

            if (!(toolbarProvider?.ShowButton(ShowBlackboardButton) ?? false))
            {
                m_ShowBlackboardButton.style.display = DisplayStyle.None;
            }
            else
            {
                m_ShowBlackboardButton.style.display = StyleKeyword.Null;
            }

            if (!(toolbarProvider?.ShowButton(EnableTracingButton) ?? false))
            {
                m_EnableTracingButton.style.display = DisplayStyle.None;
            }
            else
            {
                m_EnableTracingButton.style.display = StyleKeyword.Null;
            }
        }

        void OnNewGraphButton()
        {
            var minimap = ConsoleWindowBridge.FindBoundGraphViewToolWindow<GraphViewMinimapWindow>(m_GraphView);
            if (minimap != null)
            {
                minimap.Repaint();
            }

            m_GraphView?.Window.UnloadGraph();
        }

        static void OnSaveAllButton()
        {
            AssetDatabase.SaveAssets();
        }

        void OnBuildAllButton()
        {
            try
            {
                m_CommandDispatcher.Dispatch(new BuildAllEditorCommand());
            }
            catch (Exception e) // so the button doesn't get stuck
            {
                Debug.LogException(e);
            }
        }

        protected virtual void BuildOptionMenu(GenericMenu menu)
        {
            var prefs = m_CommandDispatcher.State.Preferences;

            if (prefs != null)
            {
                if (Unsupported.IsDeveloperMode())
                {
                    menu.AddItem(new GUIContent("Show Searcher in Regular Window"),
                        prefs.GetBool(BoolPref.SearcherInRegularWindow),
                        () =>
                        {
                            prefs.ToggleBool(BoolPref.SearcherInRegularWindow);
                        });

                    menu.AddSeparator("");

                    menu.AddItem(new GUIContent("Reload Graph"), false, () =>
                    {
                        if (m_CommandDispatcher.State?.WindowState.GraphModel != null)
                        {
                            var openedGraph = m_CommandDispatcher.State.WindowState.CurrentGraph;
                            Selection.activeObject = null;
                            Resources.UnloadAsset((Object)m_CommandDispatcher.State.WindowState.AssetModel);
                            m_CommandDispatcher.Dispatch(
                                new LoadGraphAssetCommand(openedGraph.GetGraphAssetModelPath(),
                                    openedGraph.AssetLocalId, m_GraphView.Window.PluginRepository));
                        }
                    });

                    menu.AddItem(new GUIContent("Rebuild GraphView"), false, () =>
                    {
                        using (var updater = m_CommandDispatcher.State.GraphViewState.UpdateScope)
                        {
                            updater.ForceCompleteUpdate();
                        }
                    });
                    menu.AddItem(new GUIContent("Rebuild Blackboard"), false, () =>
                    {
                        m_GraphView.GetBlackboard()?.BuildUI();
                        m_GraphView.GetBlackboard()?.UpdateFromModel();
                    });
                }
            }
        }

        void OnOptionsButton()
        {
            GenericMenu menu = new GenericMenu();
            BuildOptionMenu(menu);
            menu.ShowAsContext();
        }
    }
}
