using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    public class MathBookOnboardingProvider : OnboardingProvider
    {
        public override VisualElement CreateOnboardingElements(CommandDispatcher store)
        {
            var template = new GraphTemplate<MathBookStencil>(MathBookStencil.GraphName);
            return AddNewGraphButton<MathBookAsset>(template);
        }
    }
}
