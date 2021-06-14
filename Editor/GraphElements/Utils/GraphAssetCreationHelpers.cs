using System;
using System.IO;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    class DoCreateAsset : EndNameEditAction
    {
        CommandDispatcher m_CommandDispatcher;
        IGraphAssetModel m_AssetModel;
        IGraphTemplate m_Template;

        public void SetUp(CommandDispatcher commandDispatcher, IGraphAssetModel assetModel, IGraphTemplate template)
        {
            m_CommandDispatcher = commandDispatcher;
            m_Template = template;
            m_AssetModel = assetModel;
        }

        internal void CreateAndLoadAsset(string pathName)
        {
            AssetDatabase.CreateAsset(m_AssetModel as Object, AssetDatabase.GenerateUniqueAssetPath(pathName));
            m_AssetModel.CreateGraph(Path.GetFileNameWithoutExtension(pathName), m_Template.StencilType);
            m_Template?.InitBasicGraph(m_AssetModel.GraphModel);

            m_CommandDispatcher?.Dispatch(new LoadGraphAssetCommand(m_AssetModel));
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

    /// <summary>
    /// Helper methods to create graph assets.
    /// </summary>
    /// <typeparam name="TGraphAssetModelType">The type of graph asset model.</typeparam>
    public static class GraphAssetCreationHelpers<TGraphAssetModelType>
        where TGraphAssetModelType : ScriptableObject, IGraphAssetModel
    {
        public static IGraphAssetModel CreateInMemoryGraphAsset(Type stencilType, string name,
            IGraphTemplate graphTemplate = null)
        {
            return CreateGraphAsset(stencilType, name, null, graphTemplate);
        }

        public static IGraphAssetModel CreateGraphAsset(Type stencilType, string name, string assetPath,
            IGraphTemplate graphTemplate = null)
        {
            IGraphAssetModel graphAssetModel;

            graphAssetModel = IGraphAssetModelHelper.Create(name, assetPath, typeof(TGraphAssetModelType));
            graphAssetModel.CreateGraph(name, stencilType, assetPath != null);
            graphTemplate?.InitBasicGraph(graphAssetModel.GraphModel);

            AssetDatabase.SaveAssets();

            return graphAssetModel;
        }

        public static IGraphAssetModel PromptToCreate(IGraphTemplate template, string title, string prompt, string assetExtension)
        {
            var path = EditorUtility.SaveFilePanelInProject(title, template.DefaultAssetName, assetExtension, prompt);

            if (path.Length != 0)
            {
                string fileName = Path.GetFileNameWithoutExtension(path);
                return CreateGraphAsset(template.StencilType, fileName, path, template);
            }

            return null;
        }

        public static void CreateInProjectWindow(IGraphTemplate template, CommandDispatcher commandDispatcher, string path)
        {
            var asset = ScriptableObject.CreateInstance<TGraphAssetModelType>();
            var endAction = ScriptableObject.CreateInstance<DoCreateAsset>();
            endAction.SetUp(commandDispatcher, asset, template);

            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                asset.GetInstanceID(),
                endAction,
                $"{path}/{template.DefaultAssetName}.asset",
                AssetPreview.GetMiniThumbnail(asset),
                null);
        }
    }
}
