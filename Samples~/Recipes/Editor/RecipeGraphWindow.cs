using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Recipes
{
    public class RecipeGraphWindow : GraphViewEditorWindow
    {
        [MenuItem("GTF Samples/Recipe Editor", false)]
        public static void ShowRecipeGraphWindow()
        {
            FindOrCreateGraphWindow<RecipeGraphWindow>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            titleContent = new GUIContent("Recipe Editor");
        }

        protected override void RegisterCommandHandlers()
        {
            base.RegisterCommandHandlers();

            CommandDispatcher.RegisterCommandHandler<AddPortCommand>(AddPortCommand.DefaultHandler);
            CommandDispatcher.RegisterCommandHandler<RemovePortCommand>(RemovePortCommand.DefaultHandler);

            CommandDispatcher.RegisterCommandHandler<SetTemperatureCommand>(SetTemperatureCommand.DefaultHandler);
            CommandDispatcher.RegisterCommandHandler<SetDurationCommand>(SetDurationCommand.DefaultHandler);
        }

        protected override GraphView CreateGraphView()
        {
            return new RecipeGraphView(this, CommandDispatcher);
        }

        protected override BlankPage CreateBlankPage()
        {
            var onboardingProviders = new List<OnboardingProvider>();
            onboardingProviders.Add(new RecipeOnboardingProvider());

            return new BlankPage(CommandDispatcher, onboardingProviders);
        }
    }
}
