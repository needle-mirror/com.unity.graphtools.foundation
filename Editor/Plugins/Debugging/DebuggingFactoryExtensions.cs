using NUnit.Framework;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Plugins.Debugging
{
    [GraphElementsExtensionMethodsCache(typeof(GraphView), GraphElementsExtensionMethodsCacheAttribute.lowestPriority)]
    static class DebuggingFactoryExtensions
    {
        public static IModelUI CreateGraphProcessingErrorModelUI(this ElementBuilder elementBuilder, CommandDispatcher commandDispatcher, GraphProcessingErrorModel model)
        {
            var ui = elementBuilder.CreateErrorBadgeModelUI(commandDispatcher, model) as GraphElement;
            Assert.IsNotNull(ui);
            if (model.Fix != null)
            {
                Assert.IsNotNull(commandDispatcher);
                ui.RegisterCallback<MouseDownEvent>(e => model.Fix.QuickFixAction(commandDispatcher));
            }
            return ui;
        }
    }
}
