using System;
using UnityEditor.UIElements;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEngine;

namespace UnityEditor.VisualScripting.Editor
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

            State state = m_Store.GetState();
            IGraphModel graphModel = state.CurrentGraphModel;

            int i = 0;
            for (; i < state.EditorDataModel.PreviousGraphModels.Count; i++)
            {
                GraphModel graphToLoad = state.EditorDataModel.PreviousGraphModels[i];
                string label = graphToLoad != null ? graphToLoad.FriendlyScriptName : "<Unknown>";
                int i1 = i;
                m_Breadcrumb.CreateOrUpdateItem(i, label, () =>
                {
                    while (state.EditorDataModel.PreviousGraphModels.Count > i1)
                        state.EditorDataModel.PreviousGraphModels.RemoveAt(state.EditorDataModel.PreviousGraphModels.Count - 1);
                    m_Store.Dispatch(new LoadGraphAssetAction(graphToLoad.GetAssetPath(), loadType: LoadGraphAssetAction.Type.KeepHistory));
                });
            }

            string newCurrentGraph = graphModel?.FriendlyScriptName;
            if (newCurrentGraph != null)
                m_Breadcrumb.CreateOrUpdateItem(i++, newCurrentGraph, null);

            object boundObject = state.EditorDataModel.BoundObject;
            string newBoundObjectName = boundObject?.ToString();
            if (newBoundObjectName != null)
                m_Breadcrumb.CreateOrUpdateItem(i++, newBoundObjectName, null);

            m_Breadcrumb.TrimItems(i);
        }
    }
}
