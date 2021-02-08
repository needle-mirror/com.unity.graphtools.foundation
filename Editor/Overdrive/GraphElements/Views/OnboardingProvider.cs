using System;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public abstract class OnboardingProvider
    {
        protected const string k_PromptToCreateTitle = "Create {0}";
        protected const string k_ButtonText = "New {0}";
        protected const string k_PromptToCreate = "Create a new {0}";
        protected const string k_AssetExtension = "asset";

        protected static VisualElement AddNewGraphButton<T>(
            IGraphTemplate template,
            string promptTitle = null,
            string buttonText = null,
            string prompt = null,
            string assetExtension = k_AssetExtension) where T : ScriptableObject, IGraphAssetModel
        {
            promptTitle = promptTitle ?? string.Format(k_PromptToCreateTitle, template.GraphTypeName);
            buttonText = buttonText ?? string.Format(k_ButtonText, template.GraphTypeName);
            prompt = prompt ?? string.Format(k_PromptToCreate, template.GraphTypeName);

            var container = new VisualElement();
            container.AddToClassList("onboarding-block");

            var label = new Label(prompt);
            container.Add(label);

            var button = new Button { text = buttonText };
            button.clicked += () =>
            {
                var graphAsset = GraphAssetCreationHelpers<T>.PromptToCreate(template, promptTitle, prompt, assetExtension);
                Selection.activeObject = graphAsset as Object;
            };
            container.Add(button);

            return container;
        }

        public abstract VisualElement CreateOnboardingElements(CommandDispatcher commandDispatcher);

        public virtual bool GetGraphAndObjectFromSelection(GraphViewEditorWindow window, Object selectedObject, out string assetPath,
            out GameObject boundObject)
        {
            assetPath = null;
            boundObject = null;

            if (selectedObject is IGraphAssetModel graphAssetModel)
            {
                // don't change the current object if it's the same graph
                if (graphAssetModel == window.CommandDispatcher.GraphToolState?.GraphModel?.AssetModel)
                {
                    var currentOpenedGraph = window.CommandDispatcher.GraphToolState.WindowState.CurrentGraph;
                    assetPath = currentOpenedGraph.GraphAssetModelPath;
                    boundObject = currentOpenedGraph.BoundObject;
                    return true;
                }
            }

            return false;
        }
    }
}
