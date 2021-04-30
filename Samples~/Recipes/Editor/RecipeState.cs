using System;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Recipes
{
    public class RecipeState : GraphToolState
    {
        /// <inheritdoc />
        public RecipeState(Hash128 graphViewEditorWindowGUID, Preferences preferences)
            : base(graphViewEditorWindowGUID, preferences) { }

        /// <inheritdoc />
        public override void RegisterCommandHandlers(Dispatcher dispatcher)
        {
            base.RegisterCommandHandlers(dispatcher);

            if (!(dispatcher is CommandDispatcher commandDispatcher))
                return;

            commandDispatcher.RegisterCommandHandler<AddPortCommand>(AddPortCommand.DefaultHandler);
            commandDispatcher.RegisterCommandHandler<RemovePortCommand>(RemovePortCommand.DefaultHandler);

            commandDispatcher.RegisterCommandHandler<SetTemperatureCommand>(SetTemperatureCommand.DefaultHandler);
            commandDispatcher.RegisterCommandHandler<SetDurationCommand>(SetDurationCommand.DefaultHandler);
        }
    }
}
