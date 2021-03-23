namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Vertical
{
    [GraphElementsExtensionMethodsCache]
    static class VerticalUIFactoryExtensions
    {
        public static IModelUI CreateNode(this ElementBuilder elementBuilder, CommandDispatcher store, VerticalNodeModel model)
        {
            IModelUI ui = new VerticalNode();
            ui.SetupBuildAndUpdate(model, store, elementBuilder.GraphView, elementBuilder.Context);
            return ui;
        }
    }
}
