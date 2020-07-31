using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEditor.UIElements;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    partial class VseMenu
    {
        ToolbarBreadcrumbs m_Breadcrumb;

        void CreateBreadcrumbMenu()
        {
            m_Breadcrumb = this.MandatoryQ<ToolbarBreadcrumbs>("breadcrumb");
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
                string label = graphToLoad.GraphAssetModel?.GraphModel != null ? graphToLoad.GraphAssetModel.GraphModel.FriendlyScriptName : "<Unknown>";
                m_Breadcrumb.CreateOrUpdateItem(i, label, ClickedEvent);
            }

            string newCurrentGraph = graphModel?.FriendlyScriptName;
            if (newCurrentGraph != null)
            {
                var boundObject = (state.EditorDataModel as IEditorDataModel)?.BoundObject;
                if (boundObject != null)
                    newCurrentGraph += $" ({boundObject.name})";
                m_Breadcrumb.CreateOrUpdateItem(i, newCurrentGraph, ClickedEvent);
                i++;
            }

            m_Breadcrumb.TrimItems(i);
        }

        void ClickedEvent(int i)
        {
            var state = m_Store.GetState();
            OpenedGraph graphToLoad = default;
            if (i < state.EditorDataModel.PreviousGraphModels.Count)
                graphToLoad = state.EditorDataModel.PreviousGraphModels[i];

            while (state.EditorDataModel.PreviousGraphModels.Count > i)
                state.EditorDataModel.PreviousGraphModels.RemoveAt(
                    state.EditorDataModel.PreviousGraphModels.Count - 1);

            if (graphToLoad.GraphAssetModel != null)
                m_Store.Dispatch(new LoadGraphAssetAction(graphToLoad.GraphAssetModel.GraphModel.GetAssetPath(),
                    graphToLoad.BoundObject, loadType: LoadGraphAssetAction.Type.KeepHistory));
        }
    }
}
