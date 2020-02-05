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

            // selection is a SemanticGraph
            var semanticGraph = Selection.activeObject as GraphAssetModel;
            Object selectedObject = semanticGraph;
            if (semanticGraph != null)
            {
                SetCurrentSelection(selectedObject, OpenMode.Open);
                return;
            }

            // selection is a GameObject (not Prefab)
            selectedObject = Selection.activeGameObject;
            if (selectedObject != null)
            {
                // TODO: find matching systems ?
//                bool isPrefab = VseUtility.IsPrefabOrAsset(selectedObject);
//                if (!isPrefab)
//                {
//                    SetCurrentSelection(selectedObject, OpenMode.Open);
//                }
            }
        }

        protected void SetCurrentSelection(Object obj, OpenMode mode)
        {
            var vseWindows = (VseWindow[])Resources.FindObjectsOfTypeAll(typeof(VseWindow));

            // Only the last focused editor should try to answer a change to the current selection.
            if (s_LastFocusedEditor != GetInstanceID() && vseWindows.Length > 1)
                return;

            // Extract the selected graph asset file path from the selected object, if possible.
            string graphAssetFilePath = null;

            var editorDataModel = m_Store.GetState().EditorDataModel;
            if (editorDataModel == null)
                return;
            var curBoundObject = editorDataModel.BoundObject;
//            object player = null;
//            if (obj is GameObject)
//            {
//                var selectedGameObject = (GameObject)obj;
//
//                player = VseUtility.GetVisualScriptFromGameObject(selectedGameObject);
//                if (player != null)
//                {
//                    graphAssetFilePath = VseUtility.GetAssetPathFromComponent(player);
//                }
//            }
//            else
            if (obj is GraphAssetModel asset)
            {
                if (m_Store.GetState() != null && m_Store.GetState().AssetModel != null && (GraphAssetModel)m_Store.GetState().AssetModel == asset
                    // if we already had no bound object, abort. otherwise, we'll reload the same graph with no bound object
                    && curBoundObject == null)
                    return;

                graphAssetFilePath = AssetDatabase.GetAssetPath(asset);
            }

            // If there is not graph asset, unload the current one.
            if (string.IsNullOrWhiteSpace(graphAssetFilePath))
            {
                m_Store.Dispatch(new UnloadGraphAssetAction());
                Repaint();
                return;
            }

            // Load this graph asset.
            m_Store.Dispatch(new LoadGraphAssetAction(graphAssetFilePath));
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
