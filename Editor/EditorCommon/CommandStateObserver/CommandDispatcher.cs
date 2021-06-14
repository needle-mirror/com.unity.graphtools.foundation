using System;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Command dispatcher class for graph tools.
    /// </summary>
    public class CommandDispatcher : Dispatcher
    {
        /// <summary>
        /// The state.
        /// </summary>
        public new GraphToolState State => base.State as GraphToolState;

        internal string LastDispatchedCommandName { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandDispatcher" /> class.
        /// </summary>
        public CommandDispatcher(GraphToolState state)
            : base(state)
        {
        }

        /// <inheritdoc />
        protected override void PreDispatchCommand(ICommand command)
        {
            base.PreDispatchCommand(command);
            LastDispatchedCommandName = command.GetType().Name;
        }

        /// <inheritdoc />
        protected override bool IsDiagnosticFlagSet(Diagnostics flag)
        {
            if (flag.HasFlag(Diagnostics.LogAllCommands))
                return State.Preferences?.GetBool(BoolPref.LogAllDispatchedCommands) ?? false;

            if (flag.HasFlag(Diagnostics.CheckRecursiveDispatch))
                return State.Preferences?.GetBool(BoolPref.ErrorOnRecursiveDispatch) ?? false;

            return false;
        }

        /// <summary>
        /// Registers a handler for a command type.
        /// </summary>
        /// <param name="commandHandler">The command handler.</param>
        /// <typeparam name="TCommand">The command type.</typeparam>
        public void RegisterCommandHandler<TCommand>(CommandHandler<GraphToolState, TCommand> commandHandler)
            where TCommand : ICommand
        {
            RegisterCommandHandler<GraphToolState, TCommand>(commandHandler);
        }

    }
}
