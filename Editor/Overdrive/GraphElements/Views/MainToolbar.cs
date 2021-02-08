using System;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class MainToolbar : Toolbar
    {
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


        public MainToolbar(CommandDispatcher commandDispatcher, GraphView graphView) : base(commandDispatcher, graphView)
        {
            name = "vseMenu";
            this.AddStylesheet("MainToolbar.uss");

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
            m_EnableTracingButton.SetValueWithoutNotify(m_CommandDispatcher.GraphToolState.TracingState.TracingEnabled);
            m_EnableTracingButton.RegisterValueChangedCallback(e => m_CommandDispatcher.Dispatch(new ToggleTracingCommand(e.newValue)));

            m_OptionsButton = this.MandatoryQ<ToolbarButton>(OptionsButton);
            m_OptionsButton.tooltip = "Options";
            m_OptionsButton.ChangeClickEvent(OnOptionsButton);
        }

        public virtual void UpdateUI()
        {
            bool isEnabled = m_CommandDispatcher.GraphToolState.GraphModel != null;
            UpdateCommonMenu(isEnabled);
            UpdateBreadcrumbMenu(isEnabled);
        }

        void UpdateBreadcrumbMenu(bool isEnabled)
        {
            m_Breadcrumb.SetEnabled(isEnabled);

            var i = 0;
            var graphModels = m_CommandDispatcher.GraphToolState.WindowState.SubGraphStack;
            for (; i < graphModels.Count; i++)
            {
                var label = GetBreadcrumbLabel(i);
                m_Breadcrumb.CreateOrUpdateItem(i, label, BreadcrumbClickedEvent);
            }

            var newCurrentGraph = GetBreadcrumbLabel(-1);
            if (newCurrentGraph != null)
            {
                m_Breadcrumb.CreateOrUpdateItem(i, newCurrentGraph, BreadcrumbClickedEvent);
                i++;
            }

            m_Breadcrumb.TrimItems(i);
        }

        protected virtual string GetBreadcrumbLabel(int index)
        {
            var graphModels = m_CommandDispatcher.GraphToolState.WindowState.SubGraphStack;
            string graphName = null;
            if (index == -1)
            {
                graphName = m_CommandDispatcher.GraphToolState.WindowState.CurrentGraph.GraphName;
            }
            else if (index >= 0 && index < graphModels.Count)
            {
                graphName = graphModels[index].GraphName;
            }

            return string.IsNullOrEmpty(graphName) ? "<Unknown>" : graphName;
        }

        protected void BreadcrumbClickedEvent(int i)
        {
            var state = m_CommandDispatcher.GraphToolState;
            OpenedGraph graphToLoad = default;
            var graphModels = state.WindowState.SubGraphStack;
            if (i < graphModels.Count)
                graphToLoad = graphModels[i];

            state.WindowState.TruncateHistory(i);
            OnBreadcrumbClick(graphToLoad);
        }

        protected virtual void OnBreadcrumbClick(OpenedGraph graphToLoad)
        {
            if (graphToLoad.GraphName != null)
                m_CommandDispatcher.Dispatch(new LoadGraphAssetCommand(graphToLoad.GraphAssetModelPath,
                    graphToLoad.BoundObject, LoadGraphAssetCommand.Type.KeepHistory, graphToLoad.FileId));
        }

        void ShowGraphViewToolWindow<T>() where T : GraphViewToolWindow
        {
            var existingToolWindow = ConsoleWindowBridge.FindBoundGraphViewToolWindow<T>(m_GraphView);
            if (existingToolWindow == null)
                ConsoleWindowBridge.SpawnAttachedViewToolWindow<T>(m_GraphView.Window, m_GraphView);
            else
                existingToolWindow.Focus();
        }

        protected virtual void UpdateCommonMenu(bool enabled)
        {
            m_NewGraphButton.SetEnabled(enabled);
            m_SaveAllButton.SetEnabled(enabled);
            m_BuildAllButton.SetEnabled(enabled);

            var stencil = m_CommandDispatcher.GraphToolState?.GraphModel?.Stencil;
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

            if (!(toolbarProvider?.ShowButton(BuildAllButton) ?? true))
            {
                m_BuildAllButton.style.display = DisplayStyle.None;
            }
            else
            {
                m_BuildAllButton.style.display = StyleKeyword.Null;
            }

            if (!(toolbarProvider?.ShowButton(ShowMiniMapButton) ?? true))
            {
                m_ShowMiniMapButton.style.display = DisplayStyle.None;
            }
            else
            {
                m_ShowMiniMapButton.style.display = StyleKeyword.Null;
            }

            if (!(toolbarProvider?.ShowButton(ShowBlackboardButton) ?? true))
            {
                m_ShowBlackboardButton.style.display = DisplayStyle.None;
            }
            else
            {
                m_ShowBlackboardButton.style.display = StyleKeyword.Null;
            }
        }

        void OnNewGraphButton()
        {
            var minimap = ConsoleWindowBridge.FindBoundGraphViewToolWindow<GraphViewMinimapWindow>(m_GraphView);
            minimap?.Repaint();

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
            var prefs = m_CommandDispatcher.GraphToolState.Preferences;

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
                        if (m_CommandDispatcher.GraphToolState?.GraphModel != null)
                        {
                            var path = m_CommandDispatcher.GraphToolState.AssetModel.GetPath();
                            Selection.activeObject = null;
                            Resources.UnloadAsset((Object)m_CommandDispatcher.GraphToolState.AssetModel);
                            m_CommandDispatcher.Dispatch(new LoadGraphAssetCommand(path));
                        }
                    });

                    menu.AddItem(new GUIContent("Rebuild GraphView"), false, () =>
                    {
                        m_CommandDispatcher.MarkStateDirty();
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
