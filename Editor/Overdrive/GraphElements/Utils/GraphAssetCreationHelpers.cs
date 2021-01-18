using System;
using System.IO;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    class DoCreateAsset : EndNameEditAction
    {
        Store m_Store;
        IGraphAssetModel m_AssetModel;
        ICreatableGraphTemplate m_Template;

        public void SetUp(Store store, IGraphAssetModel assetModel, ICreatableGraphTemplate template)
        {
            m_Store = store;
            m_Template = template;
            m_AssetModel = assetModel;
        }

        internal void CreateAndLoadAsset(string pathName)
        {
            using (new AssetWatcher.Scope())
            {
                AssetDatabase.CreateAsset(m_AssetModel as Object, AssetDatabase.GenerateUniqueAssetPath(pathName));
                m_AssetModel.CreateGraph(Path.GetFileNameWithoutExtension(pathName), m_Template.StencilType);
                AssetActionHelper.InitTemplate(m_Template, m_AssetModel.GraphModel);
            }

            m_Store?.Dispatch(new LoadGraphAssetAction(m_AssetModel));
        }

        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            CreateAndLoadAsset(pathName);
        }

        public override void Cancelled(int instanceId, string pathName, string resourceFile)
        {
            Selection.activeObject = null;
        }
    }

    public static class GraphAssetCreationHelpers<TGraphAssetModelType>
        where TGraphAssetModelType : ScriptableObject, IGraphAssetModel
    {
        public static IGraphAssetModel CreateInMemoryGraphAsset(Type stencilType, string name, string assetPath,
            IGraphTemplate graphTemplate = null)
        {
            return CreateGraphAsset(stencilType, name, assetPath, graphTemplate, false);
        }

        public static IGraphAssetModel CreateGraphAsset(Type stencilType, string name, string assetPath,
            IGraphTemplate graphTemplate = null, bool writeOnDisk = true)
        {
            IGraphAssetModel graphAssetModel;

            using (new AssetWatcher.Scope())
            {
                graphAssetModel = IGraphAssetModelHelper.Create(name, assetPath, typeof(TGraphAssetModelType), writeOnDisk);
                graphAssetModel.CreateGraph(name, stencilType, writeOnDisk);
                AssetActionHelper.InitTemplate(graphTemplate, graphAssetModel.GraphModel);
            }

            AssetDatabase.SaveAssets();
            AssetWatcher.Instance.WatchGraphAssetAtPath(assetPath, graphAssetModel);

            return graphAssetModel;
        }

        public static IGraphAssetModel PromptToCreate(ICreatableGraphTemplate template, string title, string prompt, string assetExtension)
        {
            var path = EditorUtility.SaveFilePanelInProject(title, template.DefaultAssetName, assetExtension, prompt);

            if (path.Length != 0)
            {
                string fileName = Path.GetFileNameWithoutExtension(path);
                return CreateGraphAsset(template.StencilType, fileName, path, template);
            }

            return null;
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
