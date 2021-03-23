using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Recipes
{
    public class RemovePortCommand : ModelCommand<MixNodeModel>
    {
        const string k_UndoStringSingular = "Remove Ingredient";

        public RemovePortCommand(MixNodeModel[] nodes)
            : base(k_UndoStringSingular, k_UndoStringSingular, nodes)
        {
        }

        public static void DefaultHandler(GraphToolState state, RemovePortCommand command)
        {
            state.PushUndo(command);

            using (var graphUpdater = state.GraphViewState.Updater)
            {
                foreach (var nodeModel in command.Models)
                {
                    nodeModel.RemoveIngredientPort();
                    graphUpdater.U.MarkChanged(nodeModel);
                }
            }
        }
    }
}
