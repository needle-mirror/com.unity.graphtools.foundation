using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Recipes
{
    public class RecipeStencil : Stencil
    {
        public static string toolName = "Recipe Editor";

        public override string ToolName => toolName;

        public static readonly string graphName = "Recipe";
        public static TypeHandle Ingredient { get; } = TypeSerializer.GenerateCustomTypeHandle("Ingredient");
        public static TypeHandle Cookware { get; } = TypeSerializer.GenerateCustomTypeHandle("Cookware");

        public RecipeStencil() { }

        public override IGraphProcessingErrorModel CreateProcessingErrorModel(GraphProcessingError error)
        {
            if (error.SourceNode != null && !error.SourceNode.Destroyed)
            {
                return new GraphProcessingErrorModel(error);
            }

            return null;
        }
    }
}
