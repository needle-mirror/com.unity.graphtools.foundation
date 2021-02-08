using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    [GraphElementsExtensionMethodsCache]
    public static class DebuggingFactoryExtensions
    {
        public static IModelUI CreateGraphProcessingErrorBadgeModelUI(this ElementBuilder elementBuilder, CommandDispatcher commandDispatcher, GraphProcessingErrorBadgeModel model)
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
