using System;
using System.IO;
using JetBrains.Annotations;
using UnityEditor.VisualScripting.Model;
using UnityEngine;

namespace UnityEditor.VisualScripting.Editor
{
    [PublicAPI]
    public static class GraphTemplateHelpers
    {
        public static void PromptToCreate(this ICreatableGraphTemplate template, Store store)
        {
            template.PromptToCreate(store, template.GraphTypeName, template.DefaultAssetName);
        }

        public static void PromptToCreate(this IGraphTemplateFromGameObject template, Store store)
        {
            var graphTitle = template.GameObject.name;
            var assetName = graphTitle + ".asset";
            template.PromptToCreate(store, graphTitle, assetName);
        }

        public static void PromptToCreate(this IGraphTemplate template, Store store, string graphTitle, string assetName)
        {
            PromptToCreate(template.StencilType, store, graphTitle, assetName, template);
        }

        public static void PromptToCreate(Type stencilType, Store store, string graphTitle, string assetName, IGraphTemplate template = null)
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "Create visual script",
                assetName,
                "asset", "Create a new visual script for " + graphTitle);

            if (path.Length != 0)
            {
                string fileName =  Path.GetFileNameWithoutExtension(path);
                store.Dispatch(new CreateGraphAssetAction(stencilType, fileName, path, graphTemplate: template));
            }
        }
    }
}
