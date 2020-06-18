using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    public class Store<TState> : Overdrive.Store<TState> where TState : State
    {
        public Store(TState initialState)
            : base(initialState)
        {
            RegisterReducers();
        }

        void RegisterReducers()
        {
            Register<CreateEdgeAction>(CreateEdgeAction.DefaultReducer);
            Register<AddControlPointOnEdgeAction>(AddControlPointOnEdgeAction.DefaultReducer);
            Register<MoveEdgeControlPointAction>(MoveEdgeControlPointAction.DefaultReducer);
            Register<RemoveEdgeControlPointAction>(RemoveEdgeControlPointAction.DefaultReducer);
            Register<SetEdgeEditModeAction>(SetEdgeEditModeAction.DefaultReducer);
            Register<ReorderEdgeAction>(ReorderEdgeAction.DefaultReducer);

            Register<SetNodePositionAction>(SetNodePositionAction.DefaultReducer);
            Register<SetNodeCollapsedAction>(SetNodeCollapsedAction.DefaultReducer);
            Register<DropEdgeInEmptyRegionAction>(DropEdgeInEmptyRegionAction.DefaultReducer);
            Register<RenameElementAction>(RenameElementAction.DefaultReducer);

            Register<MoveElementsAction>(MoveElementsAction.DefaultReducer);
            Register<DeleteElementsAction>(DeleteElementsAction.DefaultReducer);

            Register<ChangePlacematColorAction>(ChangePlacematColorAction.DefaultReducer);
            Register<ChangePlacematZOrdersAction>(ChangePlacematZOrdersAction.DefaultReducer);
            Register<ChangePlacematPositionAction>(ChangePlacematPositionAction.DefaultReducer);
            Register<ExpandOrCollapsePlacematAction>(ExpandOrCollapsePlacematAction.DefaultReducer);

            Register<CreateStickyNoteAction>(CreateStickyNoteAction.DefaultReducer);
            Register<ResizeStickyNoteAction>(ResizeStickyNoteAction.DefaultReducer);
            Register<UpdateStickyNoteAction>(UpdateStickyNoteAction.DefaultReducer);
            Register<UpdateStickyNoteThemeAction>(UpdateStickyNoteThemeAction.DefaultReducer);
            Register<UpdateStickyNoteTextSizeAction>(UpdateStickyNoteTextSizeAction.DefaultReducer);
        }
    }
}
