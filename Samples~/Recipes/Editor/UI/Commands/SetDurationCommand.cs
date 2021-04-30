using System;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Recipes
{
    public class SetDurationCommand : ModelCommand<BakeNodeModel, int>
    {
        const string k_UndoStringSingular = "Set Bake Node Duration";
        const string k_UndoStringPlural = "Set Bake Nodes Duration";

        public SetDurationCommand(BakeNodeModel[] nodes, int value)
            : base(k_UndoStringSingular, k_UndoStringPlural, nodes, value)
        {
        }

        public static void DefaultHandler(GraphToolState state, SetDurationCommand command)
        {
            state.PushUndo(command);

            using (var graphUpdater = state.GraphViewState.UpdateScope)
            {
                foreach (var nodeModel in command.Models)
                {
                    nodeModel.Duration = command.Value;
                    graphUpdater.MarkChanged(nodeModel);
                }
            }
        }
    }
}
