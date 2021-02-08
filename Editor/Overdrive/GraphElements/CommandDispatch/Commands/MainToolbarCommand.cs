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
            var graphModel = state.GraphModel;
            if (graphModel?.Stencil == null)
                return;

            state.TracingState.TracingEnabled = command.Value;
            graphModel.Stencil.Debugger.OnToggleTracing(graphModel, command.Value);
        }
    }
}
