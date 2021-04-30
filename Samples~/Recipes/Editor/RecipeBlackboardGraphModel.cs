using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Recipes
{
    public class RecipeBlackboardGraphModel : BlackboardGraphModel
    {
        internal static readonly string[] k_Sections = { "Ingredients", "Cookware" };

        /// <inheritdoc />
        public RecipeBlackboardGraphModel(IGraphAssetModel graphAssetModel)
            : base(graphAssetModel) { }

        public override string GetBlackboardTitle()
        {
            return AssetModel?.FriendlyScriptName == null ? "Recipe" : AssetModel?.FriendlyScriptName + " Recipe";
        }

        public override string GetBlackboardSubTitle()
        {
            return "The Pantry";
        }

        public override IEnumerable<string> SectionNames =>
            GraphModel == null ? Enumerable.Empty<string>() : k_Sections;

        public override IEnumerable<IVariableDeclarationModel> GetSectionRows(string sectionName)
        {
            if (sectionName == k_Sections[0])
            {
                return GraphModel?.VariableDeclarations?.Where(v => v.DataType == RecipeStencil.Ingredient) ??
                    Enumerable.Empty<IVariableDeclarationModel>();
            }

            if (sectionName == k_Sections[1])
            {
                return GraphModel?.VariableDeclarations?.Where(v => v.DataType == RecipeStencil.Cookware) ??
                    Enumerable.Empty<IVariableDeclarationModel>();
            }

            return Enumerable.Empty<IVariableDeclarationModel>();
        }
    }
}
