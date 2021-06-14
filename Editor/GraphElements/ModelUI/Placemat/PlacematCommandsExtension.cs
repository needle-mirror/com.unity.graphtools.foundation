using System;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Extension methods to help dispatching commands for a placemat.
    /// </summary>
    static class PlacematCommandsExtension
    {
        public enum CycleDirection
        {
            Up,
            Down
        }

        public static void BringPlacematToFront(this Placemat self)
        {
            var newZOrder = self.PlacematModel.GraphModel.GetPlacematMaxZOrder() + 1;
            self.CommandDispatcher.Dispatch(
                new ChangePlacematZOrdersCommand(new[] { newZOrder }, new[] { self.PlacematModel }));
        }

        public static void SendPlacematToBack(this Placemat self)
        {
            var newZOrder = self.PlacematModel.GraphModel.GetPlacematMinZOrder() - 1;
            self.CommandDispatcher.Dispatch(
                new ChangePlacematZOrdersCommand(new[] { newZOrder }, new[] { self.PlacematModel }));
        }

        public static void CyclePlacemat(this Placemat self, CycleDirection direction)
        {
            var orderedPlacemats = self.PlacematModel.GraphModel.GetSortedPlacematModels();
            var placematToMoveIndex = orderedPlacemats.IndexOfInternal(self.PlacematModel);
            IPlacematModel placematToSwapWith = null;

            if (direction == CycleDirection.Down && placematToMoveIndex > 0)
            {
                placematToSwapWith = orderedPlacemats[placematToMoveIndex - 1];
            }
            else if (direction == CycleDirection.Up && placematToMoveIndex < orderedPlacemats.Count - 1)
            {
                placematToSwapWith = orderedPlacemats[placematToMoveIndex + 1];
            }

            if (placematToSwapWith != null)
            {
                self.CommandDispatcher.Dispatch(
                    new ChangePlacematZOrdersCommand(new[] { self.PlacematModel.ZOrder, placematToSwapWith.ZOrder }, new[] { placematToSwapWith, self.PlacematModel }));
            }
        }

        public static void CollapsePlacemat(this Placemat self, bool value)
        {
            var collapsedModels = value ? self.GatherCollapsedElements() : null;
            self.CommandDispatcher.Dispatch(new CollapsePlacematCommand(self.PlacematModel, value, collapsedModels));
        }
    }
}
