using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public static class StoreHelper
    {
        public static void RegisterDefaultReducers(Store store)
        {
            store.RegisterReducer<CreateNodeFromPortAction>(CreateNodeFromPortAction.DefaultReducer);
            store.RegisterReducer<CreateEdgeAction>(CreateEdgeAction.DefaultReducer);
            store.RegisterReducer<AddControlPointOnEdgeAction>(AddControlPointOnEdgeAction.DefaultReducer);
            store.RegisterReducer<MoveEdgeControlPointAction>(MoveEdgeControlPointAction.DefaultReducer);
            store.RegisterReducer<RemoveEdgeControlPointAction>(RemoveEdgeControlPointAction.DefaultReducer);
            store.RegisterReducer<SetEdgeEditModeAction>(SetEdgeEditModeAction.DefaultReducer);
            store.RegisterReducer<ReorderEdgeAction>(ReorderEdgeAction.DefaultReducer);
            store.RegisterReducer<SplitEdgeAndInsertExistingNodeAction>(SplitEdgeAndInsertExistingNodeAction.DefaultReducer);
            store.RegisterReducer<CreateNodeOnEdgeAction>(CreateNodeOnEdgeAction.DefaultReducer);
            store.RegisterReducer<ConvertEdgesToPortalsAction>(ConvertEdgesToPortalsAction.DefaultReducer);

            store.RegisterReducer<DisconnectNodeAction>(DisconnectNodeAction.DefaultReducer);
            store.RegisterReducer<CreateNodeFromSearcherAction>(CreateNodeFromSearcherAction.DefaultReducer);
            store.RegisterReducer<SetNodeEnabledStateAction>(SetNodeEnabledStateAction.DefaultReducer);
            store.RegisterReducer<SetNodeCollapsedAction>(SetNodeCollapsedAction.DefaultReducer);
            store.RegisterReducer<UpdateConstantNodeValueAction>(UpdateConstantNodeValueAction.DefaultReducer);

            store.RegisterReducer<CreateOppositePortalAction>(CreateOppositePortalAction.DefaultReducer);

            store.RegisterReducer<DeleteEdgeAction>(DeleteEdgeAction.DefaultReducer);
            store.RegisterReducer<BuildAllEditorAction>(BuildAllEditorAction.DefaultReducer);
            store.RegisterReducer<PasteSerializedDataAction>(PasteSerializedDataAction.DefaultReducer);

            store.RegisterReducer<AlignNodesAction>(AlignNodesAction.DefaultReducer);
            store.RegisterReducer<RenameElementAction>(RenameElementAction.DefaultReducer);
            store.RegisterReducer<DeleteElementsAction>(DeleteElementsAction.DefaultReducer);
            store.RegisterReducer<UpdatePortConstantAction>(UpdatePortConstantAction.DefaultReducer);
            store.RegisterReducer<BypassNodesAction>(BypassNodesAction.DefaultReducer);
            store.RegisterReducer<MoveElementsAction>(MoveElementsAction.DefaultReducer);
            store.RegisterReducer<AutoPlaceElementsAction>(AutoPlaceElementsAction.DefaultReducer);
            store.RegisterReducer<ChangeElementColorAction>(ChangeElementColorAction.DefaultReducer);
            store.RegisterReducer<ResetElementColorAction>(ResetElementColorAction.DefaultReducer);

            store.RegisterReducer<CreatePlacematAction>(CreatePlacematAction.DefaultReducer);
            store.RegisterReducer<ChangePlacematLayoutAction>(ChangePlacematLayoutAction.DefaultReducer);
            store.RegisterReducer<ChangePlacematZOrdersAction>(ChangePlacematZOrdersAction.DefaultReducer);
            store.RegisterReducer<SetPlacematCollapsedAction>(SetPlacematCollapsedAction.DefaultReducer);

            store.RegisterReducer<CreateStickyNoteAction>(CreateStickyNoteAction.DefaultReducer);
            store.RegisterReducer<ChangeStickyNoteLayoutAction>(ChangeStickyNoteLayoutAction.DefaultReducer);
            store.RegisterReducer<UpdateStickyNoteAction>(UpdateStickyNoteAction.DefaultReducer);
            store.RegisterReducer<UpdateStickyNoteThemeAction>(UpdateStickyNoteThemeAction.DefaultReducer);
            store.RegisterReducer<UpdateStickyNoteTextSizeAction>(UpdateStickyNoteTextSizeAction.DefaultReducer);

            store.RegisterReducer<CreateVariableNodesAction>(CreateVariableNodesAction.DefaultReducer);
            store.RegisterReducer<CreateGraphVariableDeclarationAction>(CreateGraphVariableDeclarationAction.DefaultReducer);
            store.RegisterReducer<UpdateModelPropertyValueAction>(UpdateModelPropertyValueAction.DefaultReducer);
            store.RegisterReducer<ReorderGraphVariableDeclarationAction>(ReorderGraphVariableDeclarationAction.DefaultReducer);
            store.RegisterReducer<ConvertVariableNodesToConstantNodesAction>(ConvertVariableNodesToConstantNodesAction.DefaultReducer);
            store.RegisterReducer<ConvertConstantNodesToVariableNodesAction>(ConvertConstantNodesToVariableNodesAction.DefaultReducer);
            store.RegisterReducer<ItemizeNodeAction>(ItemizeNodeAction.DefaultReducer);
            store.RegisterReducer<ToggleLockConstantNodeAction>(ToggleLockConstantNodeAction.DefaultReducer);

            store.RegisterReducer<ChangeVariableTypeAction>(ChangeVariableTypeAction.DefaultReducer);
            store.RegisterReducer<UpdateExposedAction>(UpdateExposedAction.DefaultReducer);
            store.RegisterReducer<UpdateTooltipAction>(UpdateTooltipAction.DefaultReducer);

            // PF: Dubious actions since they do not act on the model.
            store.RegisterReducer<LoadGraphAssetAction>(LoadGraphAssetAction.DefaultReducer);
        }
    }
}
