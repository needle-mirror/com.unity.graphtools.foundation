using System.Collections.Generic;
using State = UnityEditor.GraphToolsFoundation.Overdrive.GraphToolState;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    class SimpleGraphViewWindow : GraphViewEditorWindow
    {
        [InitializeOnLoadMethod]
        static void RegisterTool()
        {
            ShortcutHelper.RegisterDefaultShortcuts<SimpleGraphViewWindow>(MathBookStencil.GraphName);
        }

        [MenuItem("GTF/Samples/MathBook Editor")]
        public static void ShowWindow()
        {
            GetWindow<SimpleGraphViewWindow>();
        }

        protected override void OnEnable()
        {
            EditorToolName = "Math Book";
            base.OnEnable();
        }

        protected override GraphToolState CreateInitialState()
        {
            var prefs = Preferences.CreatePreferences(EditorToolName);
            return new MathBookState(GUID, prefs);
        }

        protected override GraphView CreateGraphView()
        {
            return new GraphView(this, CommandDispatcher, EditorToolName);
        }

        protected override BlankPage CreateBlankPage()
        {
            var onboardingProviders = new List<OnboardingProvider>();
            onboardingProviders.Add(new MathBookOnboardingProvider());

            return new BlankPage(CommandDispatcher, onboardingProviders);
        }

        protected override bool CanHandleAssetType(IGraphAssetModel asset)
        {
            return asset is MathBookAsset;
        }

        protected override MainToolbar CreateMainToolbar()
        {
            return new MathBookMainToolbar(CommandDispatcher, GraphView);
        }
    }
}
