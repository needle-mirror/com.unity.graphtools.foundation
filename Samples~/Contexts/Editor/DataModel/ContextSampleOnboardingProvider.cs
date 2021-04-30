using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.GraphToolsFoundation.Overdrive.Samples.Contexts;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Contexts
{
    public class ContextSampleOnboardingProvider : OnboardingProvider
    {
        public override VisualElement CreateOnboardingElements(CommandDispatcher store)
        {
            var template = new GraphTemplate<ContextSampleStencil>(ContextSampleStencil.GraphName);
            return AddNewGraphButton<ContextSampleAsset>(template);
        }
    }
}
