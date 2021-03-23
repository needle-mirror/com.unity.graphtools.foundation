using System;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.VisualScripting.Editor
{
    public partial class VseWindow
    {
        // DO NOT name this one "OnSelectionChange", which is a magical unity function name
        // and would automatically call this method when the selection changes.
        // we want more granular control and register it manually
        void OnGlobalSelectionChange()
        {
            // if we're in Locked mode, keep current selection
            if (Locked)
                return;

            foreach (var onboardingProvider in m_BlankPage.OnboardingProviders)
            {
                if (onboardingProvider.GetGraphAndObjectFromSelection(this, Selection.activeObject, out var selectedAssetPath, out var boundObject))
                {
                    SetCurrentSelection(selectedAssetPath, OpenMode.Open, boundObject);
                    return;
                }
            }

            // selection is a GraphAssetModel
            var semanticGraph = Selection.activeObject as GraphAssetModel;
            Object selectedObject = semanticGraph;
            if (semanticGraph != null)
            {
                SetCurrentSelection(AssetDatabase.GetAssetPath(selectedObject), OpenMode.Open);
                return;
            }
        }

        protected void SetCurrentSelection(string graphAssetFilePath, OpenMode mode, Object boundObject = null)
        {
            var vseWindows = (VseWindow[])Resources.FindObjectsOfTypeAll(typeof(VseWindow));

            // Only the last focused editor should try to answer a change to the current selection.
            if (s_LastFocusedEditor != GetInstanceID() && vseWindows.Length > 1)
                return;

            var editorDataModel = m_Store.GetState().EditorDataModel;
            if (editorDataModel == null)
                return;
            var curBoundObject = editorDataModel.BoundObject as Object;

            if (AssetDatabase.LoadAssetAtPath<GraphAssetModel>(graphAssetFilePath))
            {
                // don't load if same graph and same bound object
                if (m_Store.GetState() != null && m_Store.GetState().AssetModel != null &&
                    graphAssetFilePath == LastGraphFilePath &&
                    curBoundObject == boundObject)
                    return;
            }

            // If there is not graph asset, unload the current one.
            if (string.IsNullOrWhiteSpace(graphAssetFilePath))
            {
                return;
            }

            // Load this graph asset.
            m_Store.Dispatch(new LoadGraphAssetAction(graphAssetFilePath, boundObject));
            m_GraphView.FrameAll();

            if (mode != OpenMode.OpenAndFocus)
                return;
            // Check if an existing VSE already has this asset, if yes give it the focus.
            foreach (VseWindow vseWindow in vseWindows)
            {
                if (vseWindow.GetCurrentAssetPath() == graphAssetFilePath)
                {
                    vseWindow.Focus();
                    return;
                }
            }
        }
    }
}
