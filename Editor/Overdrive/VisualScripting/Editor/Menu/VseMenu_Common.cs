using System;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    partial class VseMenu
    {
        ToolbarButton m_NewGraphButton;
        ToolbarButton m_SaveAllButton;
        ToolbarButton m_BuildAllButton;
        ToolbarButton m_ViewInCodeViewerButton;
        ToolbarButton m_ShowMiniMapButton;
        ToolbarButton m_ShowBlackboardButton;

        public static readonly string NewGraphButton = "newGraphButton";
        public static readonly string SaveAllButton = "saveAllButton";
        public static readonly string BuildAllButton = "buildAllButton";
        public static readonly string ShowMiniMapButton = "showMiniMapButton";
        public static readonly string ShowBlackboardButton = "showBlackboardButton";

        void CreateCommonMenu()
        {
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
        }

        void ShowGraphViewToolWindow<T>() where T : GraphViewToolWindow
        {
            var existingToolWindow = ConsoleWindowBridge.FindBoundGraphViewToolWindow<T>(m_GraphView);
            if (existingToolWindow == null)
                ConsoleWindowBridge.SpawnAttachedViewToolWindow<T>(m_GraphView.window, m_GraphView);
            else
                existingToolWindow.Focus();
        }

        protected virtual void UpdateCommonMenu(VSPreferences prefs, bool enabled)
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

            EditorWindow.GetWindow<VseWindow>().UnloadGraph();
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
    }
}
