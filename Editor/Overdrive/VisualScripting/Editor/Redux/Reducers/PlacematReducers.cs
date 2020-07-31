using System;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    static class PlacematReducers
    {
        public static void Register(Store store)
        {
            store.RegisterReducer<State, CreatePlacematAction>(CreatePlacemat);
            store.RegisterReducer<State, ChangePlacematPositionAction>(ChangePlacematPosition);
            store.RegisterReducer<State, ChangePlacematZOrdersAction>(ChangePlacematZOrders);
            store.RegisterReducer<State, ExpandOrCollapsePlacematAction>(ExpandOrCollapsePlacemat);
        }

        static State CreatePlacemat(State previousState, CreatePlacematAction action)
        {
            Undo.RegisterCompleteObjectUndo((Object)previousState.AssetModel, "Create Placemat");
            EditorUtility.SetDirty((Object)previousState.AssetModel);

            previousState.CurrentGraphModel.CreatePlacemat(action.Title, action.Position);

            previousState.MarkForUpdate(UpdateFlags.GraphTopology);
            return previousState;
        }

        static State ChangePlacematPosition(State previousState, ChangePlacematPositionAction action)
        {
            if (action.ResizeFlags == ResizeFlags.None)
                return previousState;

            Undo.RegisterCompleteObjectUndo((Object)previousState.AssetModel, "Resize Placemat");
            EditorUtility.SetDirty((Object)previousState.AssetModel);
            foreach (var placematModel in action.Models)
            {
                var newRect = placematModel.PositionAndSize;
                if ((action.ResizeFlags & ResizeFlags.Left) == ResizeFlags.Left)
                {
                    newRect.x = action.Value.x;
                }
                if ((action.ResizeFlags & ResizeFlags.Top) == ResizeFlags.Top)
                {
                    newRect.y = action.Value.y;
                }
                if ((action.ResizeFlags & ResizeFlags.Width) == ResizeFlags.Width)
                {
                    newRect.width = action.Value.width;
                }
                if ((action.ResizeFlags & ResizeFlags.Height) == ResizeFlags.Height)
                {
                    newRect.height = action.Value.height;
                }
                placematModel.PositionAndSize = newRect;
                previousState.MarkForUpdate(UpdateFlags.UpdateView, placematModel);
            }
            return previousState;
        }

        static State ChangePlacematZOrders(State previousState, ChangePlacematZOrdersAction action)
        {
            Undo.RegisterCompleteObjectUndo((Object)previousState.AssetModel, "Change Placemats Ordering");
            EditorUtility.SetDirty((Object)previousState.AssetModel);

            for (var index = 0; index < action.Models.Length; index++)
            {
                var placematModel = action.Models[index];
                var zOrder = action.Value[index];
                placematModel.ZOrder = zOrder;
                previousState.MarkForUpdate(UpdateFlags.UpdateView, placematModel);
            }

            return previousState;
        }

        static State ExpandOrCollapsePlacemat(State previousState, ExpandOrCollapsePlacematAction action)
        {
            Undo.RegisterCompleteObjectUndo((Object)previousState.AssetModel, action.Collapse ? "Collapse Placemat" : "Expand Placemat");
            EditorUtility.SetDirty((Object)previousState.AssetModel);

            action.PlacematModel.Collapsed = action.Collapse;
            action.PlacematModel.HiddenElements = action.PlacematModel.Collapsed ? action.CollapsedElements : null;

            previousState.MarkForUpdate(UpdateFlags.UpdateView, action.PlacematModel);
            return previousState;
        }
    }
}
