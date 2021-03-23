using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class ToggleTracingCommand : Command
    {
        bool Value;

        public ToggleTracingCommand()
        {
            UndoString = "Toggle Tracing";
        }

        public ToggleTracingCommand(bool value) : this()
        {
            Value = value;
        }

        public static void DefaultCommandHandler(GraphToolState state, ToggleTracingCommand command)
        {
            var graphModel = state.WindowState.GraphModel;
            if (graphModel?.Stencil == null)
                return;

            using (var updater = state.TracingControlState.Updater)
            {
                updater.U.TracingEnabled = command.Value;
            }

            // PF FIXME could be implemented as an observer
            graphModel.Stencil.Debugger.OnToggleTracing(graphModel, command.Value);
        }
    }
}
