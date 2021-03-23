using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public static class CommandDispatcherHelper
    {
        public static void RegisterDefaultCommandHandlers(CommandDispatcher commandDispatcher)
        {
            commandDispatcher.RegisterCommandHandler<CreateNodeFromPortCommand>(CreateNodeFromPortCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<CreateEdgeCommand>(CreateEdgeCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<AddControlPointOnEdgeCommand>(AddControlPointOnEdgeCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<MoveEdgeControlPointCommand>(MoveEdgeControlPointCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<RemoveEdgeControlPointCommand>(RemoveEdgeControlPointCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<SetEdgeEditModeCommand>(SetEdgeEditModeCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<ReorderEdgeCommand>(ReorderEdgeCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<SplitEdgeAndInsertExistingNodeCommand>(SplitEdgeAndInsertExistingNodeCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<CreateNodeOnEdgeCommand>(CreateNodeOnEdgeCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<ConvertEdgesToPortalsCommand>(ConvertEdgesToPortalsCommand.DefaultCommandHandler);

            commandDispatcher.RegisterCommandHandler<DisconnectNodeCommand>(DisconnectNodeCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<CreateNodeFromSearcherCommand>(CreateNodeFromSearcherCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<SetNodeEnabledStateCommand>(SetNodeEnabledStateCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<SetNodeCollapsedCommand>(SetNodeCollapsedCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<UpdateConstantNodeValueCommand>(UpdateConstantNodeValueCommand.DefaultCommandHandler);

            commandDispatcher.RegisterCommandHandler<CreateOppositePortalCommand>(CreateOppositePortalCommand.DefaultCommandHandler);

            commandDispatcher.RegisterCommandHandler<DeleteEdgeCommand>(DeleteEdgeCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<BuildAllEditorCommand>(BuildAllEditorCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<PasteSerializedDataCommand>(PasteSerializedDataCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<ReframeGraphViewCommand>(ReframeGraphViewCommand.DefaultCommandHandler);

            commandDispatcher.RegisterCommandHandler<AlignNodesCommand>(AlignNodesCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<RenameElementCommand>(RenameElementCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<DeleteElementsCommand>(DeleteElementsCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<UpdatePortConstantCommand>(UpdatePortConstantCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<BypassNodesCommand>(BypassNodesCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<MoveElementsCommand>(MoveElementsCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<AutoPlaceElementsCommand>(AutoPlaceElementsCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<ChangeElementColorCommand>(ChangeElementColorCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<ResetElementColorCommand>(ResetElementColorCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<ChangeElementLayoutCommand>(ChangeElementLayoutCommand.DefaultCommandHandler);

            commandDispatcher.RegisterCommandHandler<CreatePlacematCommand>(CreatePlacematCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<TogglePortsCommand>(TogglePortsCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<ToggleEdgePortsCommand>(ToggleEdgePortsCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<ChangePlacematZOrdersCommand>(ChangePlacematZOrdersCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<SetPlacematCollapsedCommand>(SetPlacematCollapsedCommand.DefaultCommandHandler);

            commandDispatcher.RegisterCommandHandler<CreateStickyNoteCommand>(CreateStickyNoteCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<UpdateStickyNoteCommand>(UpdateStickyNoteCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<UpdateStickyNoteThemeCommand>(UpdateStickyNoteThemeCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<UpdateStickyNoteTextSizeCommand>(UpdateStickyNoteTextSizeCommand.DefaultCommandHandler);

            commandDispatcher.RegisterCommandHandler<CreateVariableNodesCommand>(CreateVariableNodesCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<CreateGraphVariableDeclarationCommand>(CreateGraphVariableDeclarationCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<UpdateModelPropertyValueCommand>(UpdateModelPropertyValueCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<ReorderGraphVariableDeclarationCommand>(ReorderGraphVariableDeclarationCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<ConvertConstantNodesAndVariableNodesCommand>(ConvertConstantNodesAndVariableNodesCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<ItemizeNodeCommand>(ItemizeNodeCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<ToggleLockConstantNodeCommand>(ToggleLockConstantNodeCommand.DefaultCommandHandler);

            commandDispatcher.RegisterCommandHandler<InitializeVariableCommand>(InitializeVariableCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<ChangeVariableTypeCommand>(ChangeVariableTypeCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<UpdateExposedCommand>(UpdateExposedCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<UpdateTooltipCommand>(UpdateTooltipCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<ExpandOrCollapseBlackboardRowCommand>(ExpandOrCollapseBlackboardRowCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<ChangeVariableDeclarationCommand>(ChangeVariableDeclarationCommand.DefaultCommandHandler);

            commandDispatcher.RegisterCommandHandler<ToggleTracingCommand>(ToggleTracingCommand.DefaultCommandHandler);

            commandDispatcher.RegisterCommandHandler<SelectElementsCommand>(SelectElementsCommand.DefaultCommandHandler);
            commandDispatcher.RegisterCommandHandler<ClearSelectionCommand>(ClearSelectionCommand.DefaultCommandHandler);

            commandDispatcher.RegisterCommandHandler<LoadGraphAssetCommand>(LoadGraphAssetCommand.DefaultCommandHandler);
        }
    }
}
