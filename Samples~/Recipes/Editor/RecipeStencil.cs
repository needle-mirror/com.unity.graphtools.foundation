namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Recipes
{
    public class RecipeStencil : Stencil
    {
        public static readonly string graphName = "Recipe";
        public static TypeHandle Ingredient { get; } = TypeSerializer.GenerateCustomTypeHandle("Ingredient");
        public static TypeHandle Cookware { get; } = TypeSerializer.GenerateCustomTypeHandle("Cookware");

        public RecipeStencil() {}
    }
}
