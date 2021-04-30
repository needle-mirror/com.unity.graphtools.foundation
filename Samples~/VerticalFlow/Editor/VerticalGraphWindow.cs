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
            EditorToolName = "Vertical Flow";
            base.OnEnable();
        }

        protected override GraphView CreateGraphView()
        {
            return new VerticalGraphView(this, CommandDispatcher, EditorToolName);
        }

        protected override BlankPage CreateBlankPage()
        {
            var onboardingProviders = new List<OnboardingProvider> { new VerticalOnboardingProvider() };

            return new BlankPage(CommandDispatcher, onboardingProviders);
        }

        /// <inheritdoc />
        protected override bool CanHandleAssetType(GraphAssetModel asset)
        {
            return asset is VerticalGraphAssetModel;
        }

        /// <inheritdoc />
        protected override GraphToolState CreateInitialState()
        {
            var prefs = Preferences.CreatePreferences(EditorToolName);
            return new VerticalGraphState(GUID, prefs);
        }
    }
}
