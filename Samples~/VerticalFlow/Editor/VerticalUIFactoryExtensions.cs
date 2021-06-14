namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Vertical
{
    [GraphElementsExtensionMethodsCache(typeof(VerticalGraphView))]
    static class VerticalUIFactoryExtensions
    {
        public static IModelUI CreateNode(this ElementBuilder elementBuilder, CommandDispatcher store, VerticalNodeModel model)
        {
            IModelUI ui = new VerticalNode();
            ui.SetupBuildAndUpdate(model, store, elementBuilder.View, elementBuilder.Context);
            return ui;
        }
    }
}
