using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class CreatePlacematAction : BaseAction
    {
        public string Title;
        public Rect Position;

        public CreatePlacematAction()
        {
            UndoString = "Create Placemat";
        }

        public CreatePlacematAction(string title, Rect position) : this()
        {
            Title = title;
            Position = position;
        }

        public static void DefaultReducer(State previousState, CreatePlacematAction action)
        {
            previousState.PushUndo(action);

            previousState.CurrentGraphModel.CreatePlacemat(action.Title, action.Position);
            previousState.MarkForUpdate(UpdateFlags.GraphTopology);
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

        public static void DefaultReducer(State previousState, ChangePlacematZOrdersAction action)
        {
            if (!action.Models.Any())
                return;

            previousState.PushUndo(action);

            for (var index = 0; index < action.Models.Count; index++)
            {
                var placematModel = action.Models[index];
                var zOrder = action.Value[index];
                placematModel.ZOrder = zOrder;
                previousState.MarkForUpdate(UpdateFlags.UpdateView, placematModel);
            }
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

        public static void DefaultReducer(State previousState, ChangePlacematLayoutAction action)
        {
            if (!action.Models.Any())
                return;

            if (action.ResizeFlags == ResizeFlags.None)
                return;

            previousState.PushUndo(action);

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

        public static void DefaultReducer(State previousState, SetPlacematCollapsedAction action)
        {
            previousState.PushUndo(action);

            action.PlacematModel.Collapsed = action.Collapse;
            action.PlacematModel.HiddenElements = action.PlacematModel.Collapsed ? action.CollapsedElements : null;

            previousState.MarkForUpdate(UpdateFlags.UpdateView, action.PlacematModel);
        }
    }
}
