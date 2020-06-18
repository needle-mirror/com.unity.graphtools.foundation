using System;
using System.IO;
using JetBrains.Annotations;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    [PublicAPI]
    public static class GraphTemplateHelpers
    {
        class DoCreateAsset : EndNameEditAction
        {
            Store m_Store;
            GraphAssetModel m_AssetModel;
            ICreatableGraphTemplate m_Template;

            public void SetUp(Store store, GraphAssetModel assetModel, ICreatableGraphTemplate template)
            {
                m_Store = store;
                m_Template = template;
                m_AssetModel = assetModel;
            }

            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                m_Store.Dispatch(new CreateGraphAssetFromModelAction(
                    m_AssetModel,
                    m_Template,
                    pathName,
                    typeof(VSGraphModel)));
            }

            public override void Cancelled(int instanceId, string pathName, string resourceFile)
            {
                Selection.activeObject = null;
            }
        }

        public static void PromptToCreate(this ICreatableGraphTemplate template, Store store)
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "Create scripting graph",
                template.DefaultAssetName,
                "asset",
                $"Create a new scripting graph for {template.GraphTypeName}");

            if (path.Length != 0)
            {
                string fileName = Path.GetFileNameWithoutExtension(path);
                store.Dispatch(new CreateGraphAssetAction(template.StencilType, fileName, path, graphTemplate: template));
            }
        }

        public static void CreateInProjectWindow(this ICreatableGraphTemplate template, Store store, string path)
        {
            var asset = (GraphAssetModel)ScriptableObject.CreateInstance<VSGraphAssetModel>();
            var endAction = ScriptableObject.CreateInstance<DoCreateAsset>();
            endAction.SetUp(store, asset, template);

            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                asset.GetInstanceID(),
                endAction,
                $"{path}/{template.DefaultAssetName}.asset",
                AssetPreview.GetMiniThumbnail(asset),
                null);
        }
    }
}
