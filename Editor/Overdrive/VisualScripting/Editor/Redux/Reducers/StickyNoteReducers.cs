using System;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    static class StickyNoteReducers
    {
        public static void Register(Store store)
        {
            store.RegisterReducer<State, CreateStickyNoteAction>(CreateStickyNote);
            store.RegisterReducer<State, ResizeStickyNoteAction>(ResizeStickyNote);
            store.RegisterReducer<State, UpdateStickyNoteAction>(UpdateStickyNote);
            store.RegisterReducer<State, UpdateStickyNoteThemeAction>(UpdateStickyNoteTheme);
            store.RegisterReducer<State, UpdateStickyNoteTextSizeAction>(UpdateStickyNoteTextSize);
        }

        static State CreateStickyNote(State previousState, CreateStickyNoteAction action)
        {
            Undo.RegisterCompleteObjectUndo((Object)previousState.AssetModel, "Add Sticky Note");
            previousState.CurrentGraphModel.CreateStickyNote(action.Position);
            return previousState;
        }

        static State ResizeStickyNote(State previousState, ResizeStickyNoteAction action)
        {
            if (action.ResizeWhat == ResizeFlags.None)
                return previousState;

            Undo.RegisterCompleteObjectUndo((Object)previousState.AssetModel, "Resize Sticky Note");
            EditorUtility.SetDirty((Object)previousState.AssetModel);

            foreach (var noteModel in action.Models)
            {
                var newRect = noteModel.PositionAndSize;
                if ((action.ResizeWhat & ResizeFlags.Left) == ResizeFlags.Left)
                {
                    newRect.x = action.Value.x;
                }
                if ((action.ResizeWhat & ResizeFlags.Top) == ResizeFlags.Top)
                {
                    newRect.y = action.Value.y;
                }
                if ((action.ResizeWhat & ResizeFlags.Width) == ResizeFlags.Width)
                {
                    newRect.width = action.Value.width;
                }
                if ((action.ResizeWhat & ResizeFlags.Height) == ResizeFlags.Height)
                {
                    newRect.height = action.Value.height;
                }

                noteModel.PositionAndSize = newRect;
                previousState.MarkForUpdate(UpdateFlags.UpdateView, noteModel);
            }

            return previousState;
        }

        static State UpdateStickyNote(State previousState, UpdateStickyNoteAction action)
        {
            if (action.Title == null && action.Contents == null)
                return previousState;

            Undo.RegisterCompleteObjectUndo((Object)previousState.AssetModel, "Change Sticky Note Content");
            EditorUtility.SetDirty((Object)previousState.AssetModel);

            if (action.Title != null)
                action.StickyNoteModel.Title = action.Title;

            if (action.Contents != null)
                action.StickyNoteModel.Contents = action.Contents;

            previousState.MarkForUpdate(UpdateFlags.UpdateView, action.StickyNoteModel);
            return previousState;
        }

        static State UpdateStickyNoteTheme(State previousState, UpdateStickyNoteThemeAction action)
        {
            Undo.RegisterCompleteObjectUndo((Object)previousState.AssetModel, "Change Sticky Note Theme");
            EditorUtility.SetDirty((Object)previousState.AssetModel);

            foreach (var noteModel in action.Models)
            {
                noteModel.Theme = action.Value;
                previousState.MarkForUpdate(UpdateFlags.UpdateView, noteModel);
            }

            return previousState;
        }

        static State UpdateStickyNoteTextSize(State previousState, UpdateStickyNoteTextSizeAction action)
        {
            Undo.RegisterCompleteObjectUndo((Object)previousState.AssetModel, "Change Sticky Note Font Size");
            EditorUtility.SetDirty((Object)previousState.AssetModel);

            foreach (var noteModel in action.Models)
            {
                noteModel.TextSize = action.Value;
                previousState.MarkForUpdate(UpdateFlags.UpdateView, noteModel);
            }

            return previousState;
        }
    }
}
