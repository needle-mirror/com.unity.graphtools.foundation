using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Recipes
{
    public class AddPortCommand : ModelCommand<MixNodeModel>
    {
        const string k_UndoStringSingular = "Add Ingredient";

        public AddPortCommand(MixNodeModel[] nodes)
            : base(k_UndoStringSingular, k_UndoStringSingular, nodes)
        {
        }

        public static void DefaultHandler(GraphToolState state, AddPortCommand command)
        {
            state.PushUndo(command);

            foreach (var nodeModel in command.Models)
            {
                nodeModel.AddIngredientPort();
                state.MarkChanged(nodeModel);
            }
        }
    }
}
