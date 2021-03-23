using UnityEditor.VisualScripting.Model;
#if UNITY_2020_1_OR_NEWER
using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace UnityEditor.VisualScripting.Editor
{
    static class PlacematReducers
    {
        public static void Register(Store store)
        {
            store.Register<CreatePlacematAction>(CreatePlacemat);
            store.Register<ChangePlacematTitleAction>(RenamePlacemat);
            store.Register<ChangePlacematPositionAction>(ChangePlacematPosition);
            store.Register<ChangePlacematZOrdersAction>(ChangePlacematZOrders);
            store.Register<ExpandOrCollapsePlacematAction>(ExpandOrCollapsePlacemat);
        }

        static State CreatePlacemat(State previousState, CreatePlacematAction action)
        {
            Undo.RegisterCompleteObjectUndo((Object)previousState.AssetModel, "Create Placemat");
            EditorUtility.SetDirty((Object)previousState.AssetModel);

            ((VSGraphModel)previousState.CurrentGraphModel).CreatePlacemat(action.Title, action.Position);

            previousState.MarkForUpdate(UpdateFlags.GraphTopology);
            return previousState;
        }

        static State RenamePlacemat(State previousState, ChangePlacematTitleAction action)
        {
            Undo.RegisterCompleteObjectUndo((Object)previousState.AssetModel, "Rename Placemat");
            EditorUtility.SetDirty((Object)previousState.AssetModel);
            foreach (var placematModel in action.PlacematModels)
            {
                placematModel.Title = action.Title;
            }
            previousState.MarkForUpdate(UpdateFlags.None);
            return previousState;
        }

        static State ChangePlacematPosition(State previousState, ChangePlacematPositionAction action)
        {
            Undo.RegisterCompleteObjectUndo((Object)previousState.AssetModel, "Resize Placemat");
            EditorUtility.SetDirty((Object)previousState.AssetModel);
            foreach (var placematModel in action.PlacematModels)
            {
                placematModel.Move(action.Position);
            }
            previousState.MarkForUpdate(UpdateFlags.None);
            return previousState;
        }

        static State ChangePlacematZOrders(State previousState, ChangePlacematZOrdersAction action)
        {
            Undo.RegisterCompleteObjectUndo((Object)previousState.AssetModel, "Change placemats zOrders");
            EditorUtility.SetDirty((Object)previousState.AssetModel);

            var models = action.PlacematModels;
            for (int i = 0; i < models.Length; i++)
            {
                models[i].ZOrder = action.ZOrders[i];
            }
            previousState.MarkForUpdate(UpdateFlags.None);
            return previousState;
        }

        static State ExpandOrCollapsePlacemat(State previousState, ExpandOrCollapsePlacematAction action)
        {
            Undo.RegisterCompleteObjectUndo((Object)previousState.AssetModel, action.Collapse ? "Collapse Placemat" : "Expand Placemat");
            EditorUtility.SetDirty((Object)previousState.AssetModel);
            action.PlacematModel.Collapsed = action.Collapse;
            if (action.PlacematModel.Collapsed)
            {
                action.PlacematModel.HiddenElementsGuid = new List<string>(action.CollapsedElements);
            }
            else
            {
                action.PlacematModel.HiddenElementsGuid = null;
            }
            previousState.MarkForUpdate(UpdateFlags.None);
            return previousState;
        }
    }
}
#endif
