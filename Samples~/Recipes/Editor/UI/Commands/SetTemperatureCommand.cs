using System;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Recipes
{
    public class SetTemperatureCommand : ModelCommand<BakeNodeModel, int>
    {
        const string k_UndoStringSingular = "Set Bake Node Temperature";
        const string k_UndoStringPlural = "Set Bake Nodes Temperature";

        public SetTemperatureCommand(BakeNodeModel[] nodes, int value)
            : base(k_UndoStringSingular, k_UndoStringPlural, nodes, value)
        {
        }

        public static void DefaultHandler(GraphToolState state, SetTemperatureCommand command)
        {
            state.PushUndo(command);

            using (var graphUpdater = state.GraphViewState.UpdateScope)
            {
                foreach (var nodeModel in command.Models)
                {
                    nodeModel.Temperature = command.Value;
                    graphUpdater.MarkChanged(nodeModel);
                }
            }
        }
    }
}
