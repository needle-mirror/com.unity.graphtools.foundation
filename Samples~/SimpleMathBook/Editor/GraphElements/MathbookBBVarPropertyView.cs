namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook.UI
{

    [GraphElementsExtensionMethodsCache]
    static class MathBookGraphElementFactoryExtensions
    {
        public static IModelUI CreateMathBookVariableDeclarationModelUI(this ElementBuilder elementBuilder, CommandDispatcher commandDispatcher, MathBookVariableDeclarationModel model)
        {
            IModelUI ui;

            if (elementBuilder.Context == BlackboardVariablePropertiesPart.blackboardVariablePropertiesPartCreationContext)
            {
                ui = new MathbookBBVarPropertyView();
                ui.SetupBuildAndUpdate(model, commandDispatcher, elementBuilder.GraphView, elementBuilder.Context);
            }
            else
            {
                ui = DefaultFactoryExtensions.CreateVariableDeclarationModelUI(elementBuilder, commandDispatcher, model);
            }

            return ui;
        }
    }

    public class MathbookBBVarPropertyView : BlackboardVariablePropertyView
    {
        protected override void BuildRows()
        {
            AddInitializationField();
            AddTooltipField();
        }

    }
}
