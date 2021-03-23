using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Vertical
{
    class VerticalGraphWindow : GraphViewEditorWindow
    {
        [InitializeOnLoadMethod]
        static void RegisterTool()
        {
            ShortcutHelper.RegisterDefaultShortcuts<VerticalGraphWindow>(VerticalStencil.toolName);
        }

        [MenuItem("GTF Samples/Vertical Flow", false)]
        public static void ShowRecipeGraphWindow()
        {
            FindOrCreateGraphWindow<VerticalGraphWindow>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            EditorToolName = "Vertical Flow";
        }

        protected override void RegisterCommandHandlers()
        {
            base.RegisterCommandHandlers();

            CommandDispatcher.RegisterCommandHandler<AddPortCommand>(AddPortCommand.DefaultHandler);
            CommandDispatcher.RegisterCommandHandler<RemovePortCommand>(RemovePortCommand.DefaultHandler);
        }

        protected override GraphView CreateGraphView()
        {
            return new VerticalGraphView(this, CommandDispatcher);
        }

        protected override BlankPage CreateBlankPage()
        {
            var onboardingProviders = new List<OnboardingProvider> { new VerticalOnboardingProvider() };

            return new BlankPage(CommandDispatcher, onboardingProviders);
        }
    }
}
