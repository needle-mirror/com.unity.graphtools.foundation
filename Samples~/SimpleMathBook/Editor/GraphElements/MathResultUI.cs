namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook.UI
{
    [GraphElementsExtensionMethodsCache(GraphElementsExtensionMethodsCacheAttribute.toolDefaultPriority)]
    public static class MathResultUIExtensions
    {
        public static IModelUI CreateMathResultUI(this ElementBuilder elementBuilder, CommandDispatcher commandDispatcher, MathResult model)
        {
            var ui = new MathResultUI();

            ui.SetupBuildAndUpdate(model, commandDispatcher, elementBuilder.GraphView, elementBuilder.Context);
            return ui;
        }
    }

    public class MathResultUI : CollapsibleInOutNode
    {
        public static readonly string printResultPartName = "print-result";

        protected override void BuildPartList()
        {
            base.BuildPartList();

            PartList.AppendPart(PrintResultPart.Create(printResultPartName, Model, this, ussClassName));
        }
    }
}
