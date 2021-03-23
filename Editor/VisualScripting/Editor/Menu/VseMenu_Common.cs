using System;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEditor.VisualScripting.Model;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VisualScripting.Editor
{
    partial class VseMenu
    {
        ToolbarButton m_NewGraphButton;
        ToolbarButton m_SaveAllButton;
        ToolbarButton m_BuildAllButton;
        ToolbarButton m_RefreshUIButton;
        ToolbarButton m_ViewInCodeViewerButton;
        ToolbarButton m_ShowMiniMapButton;
        ToolbarButton m_ShowBlackboardButton;

        void CreateCommonMenu()
        {
            m_NewGraphButton = this.MandatoryQ<ToolbarButton>("newGraphButton");
            m_NewGraphButton.tooltip = "New Graph";
            m_NewGraphButton.ChangeClickEvent(OnNewGraphButton);

            m_SaveAllButton = this.MandatoryQ<ToolbarButton>("saveAllButton");
            m_SaveAllButton.tooltip = "Save All";
            m_SaveAllButton.ChangeClickEvent(OnSaveAllButton);

            m_BuildAllButton = this.MandatoryQ<ToolbarButton>("buildAllButton");
            m_BuildAllButton.tooltip = "Build All";
            m_BuildAllButton.ChangeClickEvent(OnBuildAllButton);

            m_RefreshUIButton = this.MandatoryQ<ToolbarButton>("refreshButton");
            m_RefreshUIButton.tooltip = "Refresh UI";
            m_RefreshUIButton.ChangeClickEvent(() => m_Store.Dispatch(new RefreshUIAction(UpdateFlags.All)));

            m_ShowMiniMapButton = this.MandatoryQ<ToolbarButton>("showMiniMapButton");
            m_ShowMiniMapButton.tooltip = "Show MiniMap";
            m_ShowMiniMapButton.ChangeClickEvent(ShowGraphViewToolWindow<GraphViewMinimapWindow>);

            m_ShowBlackboardButton = this.MandatoryQ<ToolbarButton>("showBlackboardButton");
            m_ShowBlackboardButton.tooltip = "Show Blackboard";
            m_ShowBlackboardButton.ChangeClickEvent(ShowGraphViewToolWindow<GraphViewBlackboardWindow>);

            m_ViewInCodeViewerButton = this.MandatoryQ<ToolbarButton>("viewInCodeViewerButton");
            m_ViewInCodeViewerButton.tooltip = "Code Viewer";
            m_ViewInCodeViewerButton.ChangeClickEvent(OnViewInCodeViewerButton);
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

            m_ViewInCodeViewerButton.style.display = m_Store.GetState()?.AssetModel?.GraphModel?.Stencil?.GeneratesCode == true
                ? DisplayStyle.Flex
                : DisplayStyle.None;
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

        void OnViewInCodeViewerButton()
        {
            var compilationResult = m_Store.GetState()?.CompilationResultModel?.GetLastResult();
            if (compilationResult == null)
            {
                Debug.LogWarning("Compilation returned empty results");
                return;
            }

            VseUtility.UpdateCodeViewer(show: true, sourceIndex: m_GraphView.window.ToggleCodeViewPhase,
                compilationResult: compilationResult,
                selectionDelegate: lineMetadata =>
                {
                    if (lineMetadata == null)
                        return;

                    GUID nodeGuid = (GUID)lineMetadata;
                    m_Store.Dispatch(new PanToNodeAction(nodeGuid));
                });
        }
    }
}
