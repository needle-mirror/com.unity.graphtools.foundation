using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Vertical
{
    class VerticalNode : CollapsibleInOutNode
    {
        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);

            if (!(Model is VerticalNodeModel verticalNodeModel))
                return;

            if (evt.menu.MenuItems().Count > 0)
                evt.menu.AppendSeparator();

            evt.menu.AppendAction("Input/Add Port", action =>
            {
                CommandDispatcher.Dispatch(new AddPortCommand(Direction.Input, Orientation.Horizontal, verticalNodeModel));
            });

            evt.menu.AppendAction("Input/Add Vertical Port", action =>
            {
                CommandDispatcher.Dispatch(new AddPortCommand(Direction.Input, Orientation.Vertical, verticalNodeModel));
            });

            evt.menu.AppendAction("Input/Remove Port", action =>
            {
                CommandDispatcher.Dispatch(new RemovePortCommand(Direction.Input, Orientation.Horizontal, verticalNodeModel));
            }, a => verticalNodeModel.InputCount > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendAction("Input/Remove Vertical Port", action =>
            {
                CommandDispatcher.Dispatch(new RemovePortCommand(Direction.Input, Orientation.Vertical, verticalNodeModel));
            }, a => verticalNodeModel.VerticalInputCount > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendAction("Output/Add Port", action =>
            {
                CommandDispatcher.Dispatch(new AddPortCommand(Direction.Output, Orientation.Horizontal, verticalNodeModel));
            });

            evt.menu.AppendAction("Output/Add Vertical Port", action =>
            {
                CommandDispatcher.Dispatch(new AddPortCommand(Direction.Output, Orientation.Vertical, verticalNodeModel));
            });

            evt.menu.AppendAction("Output/Remove Port", action =>
            {
                CommandDispatcher.Dispatch(new RemovePortCommand(Direction.Output, Orientation.Horizontal, verticalNodeModel));
            }, a => verticalNodeModel.OutputCount > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendAction("Output/Remove Vertical Port", action =>
            {
                CommandDispatcher.Dispatch(new RemovePortCommand(Direction.Output, Orientation.Vertical, verticalNodeModel));
            }, a => verticalNodeModel.VerticalOutputCount > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }
    }
}
