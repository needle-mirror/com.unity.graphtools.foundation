using System;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

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

            using (var graphUpdater = state.GraphViewState.UpdateScope)
            {
                foreach (var nodeModel in command.Models)
                {
                    nodeModel.AddIngredientPort();
                    graphUpdater.MarkChanged(nodeModel);
                }
            }
        }
    }
}
