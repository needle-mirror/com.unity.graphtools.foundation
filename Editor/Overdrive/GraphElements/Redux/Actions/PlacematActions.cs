using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class CreatePlacematAction : BaseAction
    {
        public Rect Position;
        public string Title;

        public CreatePlacematAction()
        {
            UndoString = "Create Placemat";
        }

        public CreatePlacematAction(Rect position, string title = null) : this()
        {
            Position = position;
            Title = title;
        }

        public static void DefaultReducer(State state, CreatePlacematAction action)
        {
            state.PushUndo(action);

            var placematModel = state.GraphModel.CreatePlacemat(action.Position);
            if (action.Title != null)
                placematModel.Title = action.Title;

            state.MarkNew(placematModel);
        }
    }

    // PF: The way this action is called is flawed. PlacematContainer needs to update itself from the model.
    public class ChangePlacematZOrdersAction : ModelAction<IPlacematModel, int[]>
    {
        const string k_UndoStringSingular = "Reorder Placemat";
        const string k_UndoStringPlural = "Reorder Placemats";

        public ChangePlacematZOrdersAction() : base(k_UndoStringSingular) {}

        public ChangePlacematZOrdersAction(int[] zOrders, IPlacematModel[] placematModels)
            : base(k_UndoStringSingular, k_UndoStringPlural, placematModels, zOrders)
        {
            if (zOrders == null || placematModels == null)
                return;

            Assert.AreEqual(zOrders.Length, placematModels.Length,
                "You need to provide the same number of zOrder as placemats.");
            foreach (var zOrder in zOrders)
            {
                Assert.AreEqual(1, zOrders.Count(i => i == zOrder),
                    "Each order should be unique in the provided list.");
            }
        }

        public static void DefaultReducer(State state, ChangePlacematZOrdersAction action)
        {
            if (!action.Models.Any())
                return;

            state.PushUndo(action);

            for (var index = 0; index < action.Models.Count; index++)
            {
                var placematModel = action.Models[index];
                var zOrder = action.Value[index];
                placematModel.ZOrder = zOrder;
            }
            state.MarkChanged(action.Models);
        }
    }

    public class ChangePlacematLayoutAction : ModelAction<IPlacematModel, Rect>
    {
        const string k_UndoStringSingular = "Resize Placemat";
        const string k_UndoStringPlural = "Resize Placemats";

        public ResizeFlags ResizeFlags;

        public ChangePlacematLayoutAction()
            : base(k_UndoStringSingular) {}

        public ChangePlacematLayoutAction(Rect position, ResizeFlags resizeWhat, params IPlacematModel[] placematModels)
            : base(k_UndoStringSingular, k_UndoStringPlural, placematModels, position)
        {
            ResizeFlags = resizeWhat;
        }

        public static void DefaultReducer(State state, ChangePlacematLayoutAction action)
        {
            if (!action.Models.Any())
                return;

            if (action.ResizeFlags == ResizeFlags.None)
                return;

            state.PushUndo(action);

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
            }
            state.MarkChanged(action.Models);
        }
    }

    public class SetPlacematCollapsedAction : BaseAction
    {
        public readonly IPlacematModel PlacematModel;
        public readonly bool Collapse;
        public readonly IReadOnlyList<IGraphElementModel> CollapsedElements;

        public SetPlacematCollapsedAction()
        {
            UndoString = "Collapse Or Expand Placemat";
        }

        public SetPlacematCollapsedAction(IPlacematModel placematModel, bool collapse,
                                          IReadOnlyList<IGraphElementModel> collapsedElements) : this()
        {
            PlacematModel = placematModel;
            Collapse = collapse;
            CollapsedElements = collapsedElements;

            UndoString = Collapse ? "Collapse Placemat" : "Expand Placemat";
        }

        public static void DefaultReducer(State state, SetPlacematCollapsedAction action)
        {
            state.PushUndo(action);

            action.PlacematModel.Collapsed = action.Collapse;
            action.PlacematModel.HiddenElements = action.PlacematModel.Collapsed ? action.CollapsedElements : null;

            state.MarkChanged(action.PlacematModel);
        }
    }
}
