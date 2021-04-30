using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Recipes
{
    public class RecipeGraphWindow : GraphViewEditorWindow
    {
        [InitializeOnLoadMethod]
        static void RegisterTool()
        {
            ShortcutHelper.RegisterDefaultShortcuts<RecipeGraphWindow>(RecipeStencil.toolName);
        }

        [MenuItem("GTF Samples/Recipe Editor", false)]
        public static void ShowRecipeGraphWindow()
        {
            FindOrCreateGraphWindow<RecipeGraphWindow>();
        }

        protected override void OnEnable()
        {
            EditorToolName = "Recipe Editor";
            base.OnEnable();
        }

        /// <inheritdoc />
        protected override GraphToolState CreateInitialState()
        {
            var prefs = Preferences.CreatePreferences(EditorToolName);
            return new RecipeState(GUID, prefs);
        }

        protected override GraphView CreateGraphView()
        {
            return new RecipeGraphView(this, CommandDispatcher, EditorToolName);
        }

        protected override BlankPage CreateBlankPage()
        {
            var onboardingProviders = new List<OnboardingProvider>();
            onboardingProviders.Add(new RecipeOnboardingProvider());

            return new BlankPage(CommandDispatcher, onboardingProviders);
        }

        /// <inheritdoc />
        protected override bool CanHandleAssetType(GraphAssetModel asset)
        {
            return asset is RecipeGraphAssetModel;
        }
    }
}
