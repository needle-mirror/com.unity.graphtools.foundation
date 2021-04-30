using System.Linq;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Recipes
{
    public class RecipeStencil : Stencil
    {
        public static string toolName = "Recipe Editor";

        public override string ToolName => toolName;

        public static readonly string graphName = "Recipe";
        public static TypeHandle Ingredient { get; } = TypeSerializer.GenerateCustomTypeHandle("Ingredient");
        public static TypeHandle Cookware { get; } = TypeSerializer.GenerateCustomTypeHandle("Cookware");

        public RecipeStencil()
        {
            SetSearcherSize(SearcherService.Usage.k_CreateNode, new Vector2(375, 300), 2.0f);
        }

        /// <inheritdoc />
        public override IBlackboardGraphModel CreateBlackboardGraphModel(IGraphAssetModel graphAssetModel)
        {
            return new RecipeBlackboardGraphModel(graphAssetModel);
        }

        /// <inheritdoc />
        public override void PopulateBlackboardCreateMenu(string sectionName, GenericMenu menu, CommandDispatcher commandDispatcher)
        {
            if (sectionName == RecipeBlackboardGraphModel.k_Sections[0])
            {
                menu.AddItem(new GUIContent("Add"), false, () =>
                {
                    CreateVariableDeclaration(Ingredient.Identification, Ingredient);
                });
            }
            else if (sectionName == RecipeBlackboardGraphModel.k_Sections[1])
            {
                menu.AddItem(new GUIContent("Add"), false, () =>
                {
                    CreateVariableDeclaration(Cookware.Identification, Cookware);
                });
            }

            void CreateVariableDeclaration(string name, TypeHandle type)
            {
                var finalName = name;
                var i = 0;

                // ReSharper disable once AccessToModifiedClosure
                while (commandDispatcher.State.WindowState.GraphModel.VariableDeclarations.Any(v => v.Title == finalName))
                    finalName = name + i++;

                commandDispatcher.Dispatch(new CreateGraphVariableDeclarationCommand(finalName, true, type));
            }
        }
    }
}
