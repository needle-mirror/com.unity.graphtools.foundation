using System.Collections.Generic;
using State = UnityEditor.GraphToolsFoundation.Overdrive.GraphToolState;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Contexts.UI
{
    internal class ContextGraphViewWindow : GraphViewEditorWindow
    {
        [InitializeOnLoadMethod]
        static void RegisterTool()
        {
            ShortcutHelper.RegisterDefaultShortcuts<ContextGraphViewWindow>(ContextSampleStencil.GraphName);
        }

        [MenuItem("GTF/Samples/Contexts Editor")]
        public static void ShowWindow()
        {
            GetWindow<ContextGraphViewWindow>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            EditorToolName = "Contexts";
        }

        /// <inheritdoc />
        protected override GraphToolState CreateInitialState()
        {
            var prefs = Preferences.CreatePreferences(EditorToolName);
            return new ContextSampleState(GUID, prefs);
        }

        protected override GraphView CreateGraphView()
        {
            return new ContextGraphView(this, true, CommandDispatcher);
        }

        protected override BlankPage CreateBlankPage()
        {
            var onboardingProviders = new List<OnboardingProvider>();
            onboardingProviders.Add(new ContextSampleOnboardingProvider());

            return new BlankPage(CommandDispatcher, onboardingProviders);
        }

        protected override bool CanHandleAssetType(IGraphAssetModel asset)
        {
            return asset is ContextSampleAsset;
        }
    }
}
