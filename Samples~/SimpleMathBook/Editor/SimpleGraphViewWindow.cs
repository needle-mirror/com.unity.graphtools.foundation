using System.Collections.Generic;
using State = UnityEditor.GraphToolsFoundation.Overdrive.GraphToolState;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    internal class SimpleGraphViewWindow : GraphViewEditorWindow
    {
        [InitializeOnLoadMethod]
        static void RegisterTool()
        {
            ShortcutHelper.RegisterDefaultShortcuts<SimpleGraphViewWindow>(MathBookStencil.GraphName);
        }

        [MenuItem("GTF Samples/MathBook Editor")]
        public static void ShowWindow()
        {
            GetWindow<SimpleGraphViewWindow>();
        }

        protected override void OnEnable()
        {
            EditorToolName = "Math Book";
            base.OnEnable();
        }

        protected override GraphView CreateGraphView()
        {
            return new SimpleGraphView(this, CommandDispatcher, EditorToolName);
        }

        protected override BlankPage CreateBlankPage()
        {
            var onboardingProviders = new List<OnboardingProvider>();
            onboardingProviders.Add(new MathBookOnboardingProvider());

            return new BlankPage(CommandDispatcher, onboardingProviders);
        }

        protected override bool CanHandleAssetType(GraphAssetModel asset)
        {
            return asset is MathBookAsset;
        }

        protected override MainToolbar CreateMainToolbar()
        {
            return new MathBookMainToolbar(CommandDispatcher, GraphView);
        }
    }
}
