using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Recipes
{
    [GraphElementsExtensionMethodsCache]
    public static class RecipeUIFactoryExtensions
    {
        public static IModelUI CreateNode(this ElementBuilder elementBuilder, CommandDispatcher store, MixNodeModel model)
        {
            IModelUI ui = new VariableIngredientNode();
            ui.SetupBuildAndUpdate(model, store, elementBuilder.GraphView, elementBuilder.Context);
            return ui;
        }

        public static IModelUI CreateNode(this ElementBuilder elementBuilder, CommandDispatcher store, BakeNodeModel model)
        {
            IModelUI ui = new BakeNode();
            ui.SetupBuildAndUpdate(model, store, elementBuilder.GraphView, elementBuilder.Context);
            return ui;
        }
    }
}
