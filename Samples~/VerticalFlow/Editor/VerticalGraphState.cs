using System;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Vertical
{
    class VerticalGraphState : GraphToolState
    {
        /// <inheritdoc />
        public VerticalGraphState(Hash128 graphViewEditorWindowGUID, Preferences preferences)
            : base(graphViewEditorWindowGUID, preferences) { }

        /// <inheritdoc />
        public override void RegisterCommandHandlers(Dispatcher dispatcher)
        {
            base.RegisterCommandHandlers(dispatcher);

            if (!(dispatcher is CommandDispatcher commandDispatcher))
                return;

            commandDispatcher.RegisterCommandHandler<AddPortCommand>(AddPortCommand.DefaultHandler);
            commandDispatcher.RegisterCommandHandler<RemovePortCommand>(RemovePortCommand.DefaultHandler);
        }
    }
}
