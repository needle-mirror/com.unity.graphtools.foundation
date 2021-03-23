using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Plugins.Debugging
{
    [GraphElementsExtensionMethodsCache(GraphElementsExtensionMethodsCacheAttribute.lowestPriority)]
    public static class GraphElementFactoryExtensions
    {
        public static IModelUI CreatePort(this ElementBuilder elementBuilder, CommandDispatcher commandDispatcher, PortModel model)
        {
            var ui = new DebuggingPort();
            ui.SetupBuildAndUpdate(model, commandDispatcher, elementBuilder.GraphView, elementBuilder.Context);
            return ui;
        }
    }
}
