using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    public static class StoreHelper
    {
        public static void RegisterReducers(Store store)
        {
            store.RegisterReducer<State, CreateEdgeAction>(CreateEdgeAction.DefaultReducer);
            store.RegisterReducer<State, AddControlPointOnEdgeAction>(AddControlPointOnEdgeAction.DefaultReducer);
            store.RegisterReducer<State, MoveEdgeControlPointAction>(MoveEdgeControlPointAction.DefaultReducer);
            store.RegisterReducer<State, RemoveEdgeControlPointAction>(RemoveEdgeControlPointAction.DefaultReducer);
            store.RegisterReducer<State, SetEdgeEditModeAction>(SetEdgeEditModeAction.DefaultReducer);
            store.RegisterReducer<State, ReorderEdgeAction>(ReorderEdgeAction.DefaultReducer);

            store.RegisterReducer<State, SetNodePositionAction>(SetNodePositionAction.DefaultReducer);
            store.RegisterReducer<State, SetNodeCollapsedAction>(SetNodeCollapsedAction.DefaultReducer);
            store.RegisterReducer<State, DropEdgeInEmptyRegionAction>(DropEdgeInEmptyRegionAction.DefaultReducer);
            store.RegisterReducer<State, RenameElementAction>(RenameElementAction.DefaultReducer);

            store.RegisterReducer<State, MoveElementsAction>(MoveElementsAction.DefaultReducer);
            store.RegisterReducer<State, DeleteElementsAction>(DeleteElementsAction.DefaultReducer);

            store.RegisterReducer<State, AutoPlaceElementsAction>(AutoPlaceElementsAction.DefaultReducer);

            store.RegisterReducer<State, ChangePlacematColorAction>(ChangePlacematColorAction.DefaultReducer);
            store.RegisterReducer<State, ChangePlacematZOrdersAction>(ChangePlacematZOrdersAction.DefaultReducer);
            store.RegisterReducer<State, ChangePlacematPositionAction>(ChangePlacematPositionAction.DefaultReducer);
            store.RegisterReducer<State, ExpandOrCollapsePlacematAction>(ExpandOrCollapsePlacematAction.DefaultReducer);

            store.RegisterReducer<State, CreateStickyNoteAction>(CreateStickyNoteAction.DefaultReducer);
            store.RegisterReducer<State, ResizeStickyNoteAction>(ResizeStickyNoteAction.DefaultReducer);
            store.RegisterReducer<State, UpdateStickyNoteAction>(UpdateStickyNoteAction.DefaultReducer);
            store.RegisterReducer<State, UpdateStickyNoteThemeAction>(UpdateStickyNoteThemeAction.DefaultReducer);
            store.RegisterReducer<State, UpdateStickyNoteTextSizeAction>(UpdateStickyNoteTextSizeAction.DefaultReducer);
        }
    }
}
