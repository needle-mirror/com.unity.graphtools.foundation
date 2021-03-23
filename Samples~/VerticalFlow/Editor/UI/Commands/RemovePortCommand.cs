using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Vertical
{
    class RemovePortCommand : ModelCommand<VerticalNodeModel>
    {
        const string k_UndoStringSingular = "Remove Port";

        readonly Direction m_PortDirection;
        readonly Orientation m_PortOrientation;

        public RemovePortCommand(Direction direction, Orientation orientation, params VerticalNodeModel[] nodes)
            : base(k_UndoStringSingular, k_UndoStringSingular, nodes)
        {
            m_PortDirection = direction;
            m_PortOrientation = orientation;
        }

        public static void DefaultHandler(GraphToolState state, RemovePortCommand command)
        {
            if (!command.Models.Any() || command.m_PortDirection == Direction.None)
                return;

            state.PushUndo(command);

            using (var updater = state.GraphViewState.Updater)
            {
                foreach (var nodeModel in command.Models)
                    nodeModel.RemovePort(command.m_PortOrientation, command.m_PortDirection);

                updater.U.MarkChanged(command.Models);
            }
        }
    }
}