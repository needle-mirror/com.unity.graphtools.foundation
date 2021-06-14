using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Recipes
{
    [GraphElementsExtensionMethodsCache(typeof(RecipeGraphView))]
    public static class RecipeGraphViewFactoryExtensions
    {
        public static IModelUI CreateNode(this ElementBuilder elementBuilder, CommandDispatcher dispatcher, MixNodeModel model)
        {
            IModelUI ui = new VariableIngredientNode();
            ui.SetupBuildAndUpdate(model, dispatcher, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        public static IModelUI CreateNode(this ElementBuilder elementBuilder, CommandDispatcher dispatcher, BakeNodeModel model)
        {
            IModelUI ui = new BakeNode();
            ui.SetupBuildAndUpdate(model, dispatcher, elementBuilder.View, elementBuilder.Context);
            return ui;
        }
    }
}
