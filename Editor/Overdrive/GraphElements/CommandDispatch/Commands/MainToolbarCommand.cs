using System.Collections.Generic;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class ToggleTracingCommand : UndoableCommand
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

            using (var updater = state.TracingStatusState.UpdateScope)
            {
                updater.TracingEnabled = command.Value;
            }

            // PF FIXME could be implemented as an observer
            ((Stencil)graphModel.Stencil)?.Debugger.OnToggleTracing(graphModel, command.Value);
        }
    }
}
