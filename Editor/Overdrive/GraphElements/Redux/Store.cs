using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    public class Store : Overdrive.Store
    {
        public Store(State initialState)
            : base(initialState)
        {
            RegisterReducers();
        }

        void RegisterReducers()
        {
            RegisterReducer<State, CreateEdgeAction>(CreateEdgeAction.DefaultReducer);
            RegisterReducer<State, AddControlPointOnEdgeAction>(AddControlPointOnEdgeAction.DefaultReducer);
            RegisterReducer<State, MoveEdgeControlPointAction>(MoveEdgeControlPointAction.DefaultReducer);
            RegisterReducer<State, RemoveEdgeControlPointAction>(RemoveEdgeControlPointAction.DefaultReducer);
            RegisterReducer<State, SetEdgeEditModeAction>(SetEdgeEditModeAction.DefaultReducer);
            RegisterReducer<State, ReorderEdgeAction>(ReorderEdgeAction.DefaultReducer);

            RegisterReducer<State, SetNodePositionAction>(SetNodePositionAction.DefaultReducer);
            RegisterReducer<State, SetNodeCollapsedAction>(SetNodeCollapsedAction.DefaultReducer);
            RegisterReducer<State, DropEdgeInEmptyRegionAction>(DropEdgeInEmptyRegionAction.DefaultReducer);
            RegisterReducer<State, RenameElementAction>(RenameElementAction.DefaultReducer);

            RegisterReducer<State, MoveElementsAction>(MoveElementsAction.DefaultReducer);
            RegisterReducer<State, DeleteElementsAction>(DeleteElementsAction.DefaultReducer);

            RegisterReducer<State, AlignElementsAction>(AlignElementsAction.DefaultReducer);

            RegisterReducer<State, ChangePlacematColorAction>(ChangePlacematColorAction.DefaultReducer);
            RegisterReducer<State, ChangePlacematZOrdersAction>(ChangePlacematZOrdersAction.DefaultReducer);
            RegisterReducer<State, ChangePlacematPositionAction>(ChangePlacematPositionAction.DefaultReducer);
            RegisterReducer<State, ExpandOrCollapsePlacematAction>(ExpandOrCollapsePlacematAction.DefaultReducer);

            RegisterReducer<State, CreateStickyNoteAction>(CreateStickyNoteAction.DefaultReducer);
            RegisterReducer<State, ResizeStickyNoteAction>(ResizeStickyNoteAction.DefaultReducer);
            RegisterReducer<State, UpdateStickyNoteAction>(UpdateStickyNoteAction.DefaultReducer);
            RegisterReducer<State, UpdateStickyNoteThemeAction>(UpdateStickyNoteThemeAction.DefaultReducer);
            RegisterReducer<State, UpdateStickyNoteTextSizeAction>(UpdateStickyNoteTextSizeAction.DefaultReducer);
        }
    }
}
