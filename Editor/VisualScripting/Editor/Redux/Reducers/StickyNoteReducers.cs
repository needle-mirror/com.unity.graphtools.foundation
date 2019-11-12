using System;
using System.Linq;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.VisualScripting.Editor
{
    static class StickyNoteReducers
    {
        public static void Register(Store store)
        {
            store.Register<CreateStickyNoteAction>(CreateStickyNote);
            store.Register<ResizeStickyNoteAction>(ResizeStickyNote);
            store.Register<UpdateStickyNoteAction>(UpdateStickyNote);
            store.Register<UpdateStickyNoteThemeAction>(UpdateStickyNoteTheme);
            store.Register<UpdateStickyNoteTextSizeAction>(UpdateStickyNoteTextSize);
        }

        static State CreateStickyNote(State previousState, CreateStickyNoteAction action)
        {
            ((VSGraphModel)previousState.CurrentGraphModel).CreateStickyNote(action.Position);
            return previousState;
        }

        static State ResizeStickyNote(State previousState, ResizeStickyNoteAction action)
        {
            var stickyNoteModel = (StickyNoteModel)action.StickyNoteModel;
            Undo.RegisterCompleteObjectUndo(stickyNoteModel.SerializableAsset, "Resize StickyNote");
            stickyNoteModel.Move(action.Position);
            previousState.MarkForUpdate(UpdateFlags.GraphGeometry);
            return previousState;
        }

        static State UpdateStickyNote(State previousState, UpdateStickyNoteAction action)
        {
            var stickyNoteModel = (StickyNoteModel)action.StickyNoteModel;
            Undo.RegisterCompleteObjectUndo(stickyNoteModel.SerializableAsset, "Update Basic Settings");
            stickyNoteModel.UpdateBasicSettings(action.Title, action.Contents);
            VSGraphModel graphModel = (VSGraphModel)previousState.CurrentGraphModel;
            graphModel.LastChanges.ChangedElements.Add(stickyNoteModel);
            return previousState;
        }

        static State UpdateStickyNoteTheme(State previousState, UpdateStickyNoteThemeAction action)
        {
            VSGraphModel graphModel = (VSGraphModel)previousState.CurrentGraphModel;
            Undo.RegisterCompleteObjectUndo((Object)graphModel.AssetModel, "Update theme");
            foreach (var stickyNoteModel in action.StickyNoteModels.OfType<StickyNoteModel>())
            {
                stickyNoteModel.UpdateTheme(action.Theme);
                graphModel.LastChanges.ChangedElements.Add(stickyNoteModel);
            }

            return previousState;
        }

        static State UpdateStickyNoteTextSize(State previousState, UpdateStickyNoteTextSizeAction action)
        {
            VSGraphModel graphModel = (VSGraphModel)previousState.CurrentGraphModel;
            Undo.RegisterCompleteObjectUndo((Object)graphModel.AssetModel, "Update Text Size");
            foreach (var stickyNoteModel in action.StickyNoteModels.OfType<StickyNoteModel>())
            {
                stickyNoteModel.UpdateTextSize(action.TextSize);
                graphModel.LastChanges.ChangedElements.Add(stickyNoteModel);
            }

            previousState.MarkForUpdate(UpdateFlags.GraphTopology);
            return previousState;
        }
    }
}
