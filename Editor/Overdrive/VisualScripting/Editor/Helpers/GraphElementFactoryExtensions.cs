using System;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    [GraphElementsExtensionMethodsCache(GraphElementsExtensionMethodsCacheAttribute.lowestPriority)]
    public static class GraphElementFactoryExtensions
    {
        public static IModelUI CreatePort(this ElementBuilder elementBuilder, CommandDispatcher commandDispatcher, PortModel model)
        {
            var ui = new VisualScripting.Port();
            ui.SetupBuildAndUpdate(model, commandDispatcher, elementBuilder.GraphView, elementBuilder.Context);
            return ui;
        }
    }
}
