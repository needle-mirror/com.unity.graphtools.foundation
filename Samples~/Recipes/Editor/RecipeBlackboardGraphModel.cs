using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Recipes
{
    public class RecipeBlackboardGraphModel : BlackboardGraphModel
    {
        static readonly string[] k_Sections = { "Ingredients", "Cookware" };

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

        public override void PopulateCreateMenu(string sectionName, GenericMenu menu, CommandDispatcher commandDispatcher)
        {
            if (sectionName == k_Sections[0])
            {
                menu.AddItem(new GUIContent("Add"), false, () =>
                {
                    CreateVariableDeclaration(commandDispatcher, RecipeStencil.Ingredient.Identification, RecipeStencil.Ingredient);
                });
            }
            else if (sectionName == k_Sections[1])
            {
                menu.AddItem(new GUIContent("Add"), false, () =>
                {
                    CreateVariableDeclaration(commandDispatcher, RecipeStencil.Cookware.Identification, RecipeStencil.Cookware);
                });
            }
        }

        static void CreateVariableDeclaration(CommandDispatcher commandDispatcher, string name, TypeHandle type)
        {
            var finalName = name;
            var i = 0;

            // ReSharper disable once AccessToModifiedClosure
            while (commandDispatcher.GraphToolState.GraphModel.VariableDeclarations.Any(v => v.Title == finalName))
                finalName = name + i++;

            commandDispatcher.Dispatch(new CreateGraphVariableDeclarationCommand(finalName, true, type));
        }
    }
}
