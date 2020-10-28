using System;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class MainToolbar : Toolbar
    {
        public Action<ChangeEvent<bool>> OnToggleTracing;

        protected ToolbarBreadcrumbs m_Breadcrumb;
        protected ToolbarButton m_NewGraphButton;
        protected ToolbarButton m_SaveAllButton;
        protected ToolbarButton m_BuildAllButton;
        protected ToolbarButton m_ShowMiniMapButton;
        protected ToolbarButton m_ShowBlackboardButton;

        public static readonly string NewGraphButton = "newGraphButton";
        public static readonly string SaveAllButton = "saveAllButton";
        public static readonly string BuildAllButton = "buildAllButton";
        public static readonly string ShowMiniMapButton = "showMiniMapButton";
        public static readonly string ShowBlackboardButton = "showBlackboardButton";

        public MainToolbar(Store store, GraphView graphView) : base(store, graphView)
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

            var enableTracingButton = this.MandatoryQ<ToolbarToggle>("enableTracingButton");
            enableTracingButton.tooltip = "Toggle Tracing For Current Instance";
            enableTracingButton.SetValueWithoutNotify(m_Store.GetState().EditorDataModel.TracingEnabled);
            enableTracingButton.RegisterValueChangedCallback(e => OnToggleTracing?.Invoke(e));

            var optionsButton = this.MandatoryQ<ToolbarButton>("optionsButton");
            optionsButton.tooltip = "Options";
            optionsButton.RemoveManipulator(optionsButton.clickable);
            optionsButton.AddManipulator(new Clickable(OnOptionsButton));
        }

        public virtual void UpdateUI()
        {
            bool isEnabled = m_Store.GetState().CurrentGraphModel != null;
            UpdateCommonMenu(isEnabled);
            UpdateBreadcrumbMenu(isEnabled);
        }

        void UpdateBreadcrumbMenu(bool isEnabled)
        {
            m_Breadcrumb.SetEnabled(isEnabled);

            var state = m_Store.GetState();
            var graphModel = state.CurrentGraphModel;

            int i = 0;
            for (; i < state.EditorDataModel.PreviousGraphModels.Count; i++)
            {
                var graphToLoad = state.EditorDataModel.PreviousGraphModels[i];
                string label = GetBreadcrumbLabel(graphToLoad.GraphAssetModel?.GraphModel, i, false);
                m_Breadcrumb.CreateOrUpdateItem(i, label, BreadcrumbClickedEvent);
            }

            string newCurrentGraph = GetBreadcrumbLabel(graphModel, i, true);
            if (newCurrentGraph != null)
            {
                m_Breadcrumb.CreateOrUpdateItem(i, newCurrentGraph, BreadcrumbClickedEvent);
                i++;
            }

            m_Breadcrumb.TrimItems(i);
        }

        protected virtual string GetBreadcrumbLabel(IGraphModel graph, int index, bool isLastItem)
        {
            return graph != null ? graph.FriendlyScriptName : "<Unknown>";
        }

        protected void BreadcrumbClickedEvent(int i)
        {
            var state = m_Store.GetState();
            OpenedGraph graphToLoad = default;
            if (i < state.EditorDataModel.PreviousGraphModels.Count)
                graphToLoad = state.EditorDataModel.PreviousGraphModels[i];

            while (state.EditorDataModel.PreviousGraphModels.Count > i)
                state.EditorDataModel.PreviousGraphModels.RemoveAt(
                    state.EditorDataModel.PreviousGraphModels.Count - 1);

            if (graphToLoad.GraphAssetModel != null)
                m_Store.Dispatch(new LoadGraphAssetAction(graphToLoad.GraphAssetModel,
                    graphToLoad.BoundObject, loadType: LoadGraphAssetAction.Type.KeepHistory));
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

            var stencil = m_Store.GetState()?.CurrentGraphModel?.Stencil;
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
            if (minimap != null)
                minimap.Close();

            var bb = ConsoleWindowBridge.FindBoundGraphViewToolWindow<GraphViewBlackboardWindow>(m_GraphView);
            if (bb != null)
                bb.Close();

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
                m_Store.Dispatch(new BuildAllEditorAction());
            }
            catch (Exception e) // so the button doesn't get stuck
            {
                Debug.LogException(e);
            }
        }

        protected virtual void BuildOptionMenu(GenericMenu menu)
        {
            var prefs = m_Store.GetState().Preferences;

            if (prefs != null)
            {
                if (Unsupported.IsDeveloperMode())
                {
                    menu.AddItem(new GUIContent("Display Searcher in regular window"),
                        prefs.GetBool(BoolPref.SearcherInRegularWindow),
                        () =>
                        {
                            prefs.ToggleBool(BoolPref.SearcherInRegularWindow);
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
