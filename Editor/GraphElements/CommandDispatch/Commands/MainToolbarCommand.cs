using System.Collections.Generic;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Command to set tracing on or off.
    /// </summary>
    public class ActivateTracingCommand : UndoableCommand
    {
        /// <summary>
        /// Whether tracing should be active or not.
        /// </summary>
        public bool Value;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActivateTracingCommand"/> class.
        /// </summary>
        public ActivateTracingCommand()
        {
            UndoString = "Activate Tracing";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActivateTracingCommand"/> class.
        /// </summary>
        /// <param name="value">True if tracing should be activated, false otherwise.</param>
        public ActivateTracingCommand(bool value) : this()
        {
            Value = value;

            if (!Value)
            {
                UndoString = "Deactivate Tracing";
            }
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(GraphToolState state, ActivateTracingCommand command)
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
