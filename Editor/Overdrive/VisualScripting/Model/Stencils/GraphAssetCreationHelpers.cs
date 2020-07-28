using System;
using System.IO;
using JetBrains.Annotations;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    class DoCreateAsset : EndNameEditAction
    {
        Store m_Store;
        IGTFGraphAssetModel m_AssetModel;
        ICreatableGraphTemplate m_Template;

        public void SetUp(Store store, IGTFGraphAssetModel assetModel, ICreatableGraphTemplate template)
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
                pathName));
        }

        public override void Cancelled(int instanceId, string pathName, string resourceFile)
        {
            Selection.activeObject = null;
        }
    }

    [PublicAPI]
    public static class GraphAssetCreationHelpers<TGraphAssetModelType>
        where TGraphAssetModelType : ScriptableObject, IGTFGraphAssetModel
    {
        public static void PromptToCreate(ICreatableGraphTemplate template, Store store)
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "Create scripting graph",
                template.DefaultAssetName,
                "asset",
                $"Create a new scripting graph for {template.GraphTypeName}");

            if (path.Length != 0)
            {
                string fileName = Path.GetFileNameWithoutExtension(path);
                store.Dispatch(new CreateGraphAssetAction(template.StencilType, typeof(TGraphAssetModelType), fileName, path, graphTemplate: template));
            }
        }

        public static void CreateInProjectWindow(ICreatableGraphTemplate template, Store store, string path)
        {
            var asset = ScriptableObject.CreateInstance<TGraphAssetModelType>();
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
