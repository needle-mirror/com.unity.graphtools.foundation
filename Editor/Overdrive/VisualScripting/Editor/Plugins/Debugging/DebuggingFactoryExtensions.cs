using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    [GraphElementsExtensionMethodsCache]
    public static class DebuggingFactoryExtensions
    {
        public static IGraphElement CreateCompileErrorBadgeModelUI(this ElementBuilder elementBuilder, Store store, CompilerErrorBadgeModel model)
        {
            var ui = elementBuilder.CreateErrorBadgeModelUI(store, model) as GraphElement;
            Assert.IsNotNull(ui);
            if (model.QuickFix != null)
            {
                Assert.IsNotNull(store);
                ui.RegisterCallback<MouseDownEvent>(e => model.QuickFix.quickFix(store));
            }
            return ui;
        }
    }
}
