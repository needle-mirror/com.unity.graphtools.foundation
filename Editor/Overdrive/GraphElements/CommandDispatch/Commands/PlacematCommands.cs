using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class CreatePlacematCommand : Command
    {
        public Rect Position;
        public string Title;

        public CreatePlacematCommand()
        {
            UndoString = "Create Placemat";
        }

        public CreatePlacematCommand(Rect position, string title = null) : this()
        {
            Position = position;
            Title = title;
        }

        public static void DefaultCommandHandler(GraphToolState graphToolState, CreatePlacematCommand command)
        {
            graphToolState.PushUndo(command);

            var placematModel = graphToolState.GraphModel.CreatePlacemat(command.Position);
            if (command.Title != null)
                placematModel.Title = command.Title;

            graphToolState.MarkNew(placematModel);
        }
    }

    // PF: The way this command is called is flawed. PlacematContainer needs to update itself from the model.
    public class ChangePlacematZOrdersCommand : ModelCommand<IPlacematModel, int[]>
    {
        const string k_UndoStringSingular = "Reorder Placemat";
        const string k_UndoStringPlural = "Reorder Placemats";

        public ChangePlacematZOrdersCommand() : base(k_UndoStringSingular) {}

        public ChangePlacematZOrdersCommand(int[] zOrders, IPlacematModel[] placematModels)
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

        public static void DefaultCommandHandler(GraphToolState graphToolState, ChangePlacematZOrdersCommand command)
        {
            if (!command.Models.Any())
                return;

            graphToolState.PushUndo(command);

            for (var index = 0; index < command.Models.Count; index++)
            {
                var placematModel = command.Models[index];
                var zOrder = command.Value[index];
                placematModel.ZOrder = zOrder;
            }
            graphToolState.MarkChanged(command.Models);
        }
    }

    public class ChangePlacematLayoutCommand : ModelCommand<IPlacematModel, Rect>
    {
        const string k_UndoStringSingular = "Resize Placemat";
        const string k_UndoStringPlural = "Resize Placemats";

        public ResizeFlags ResizeFlags;

        public ChangePlacematLayoutCommand()
            : base(k_UndoStringSingular) {}

        public ChangePlacematLayoutCommand(Rect position, ResizeFlags resizeWhat, params IPlacematModel[] placematModels)
            : base(k_UndoStringSingular, k_UndoStringPlural, placematModels, position)
        {
            ResizeFlags = resizeWhat;
        }

        public static void DefaultCommandHandler(GraphToolState graphToolState, ChangePlacematLayoutCommand command)
        {
            if (!command.Models.Any())
                return;

            if (command.ResizeFlags == ResizeFlags.None)
                return;

            graphToolState.PushUndo(command);

            foreach (var placematModel in command.Models)
            {
                var newRect = placematModel.PositionAndSize;
                if ((command.ResizeFlags & ResizeFlags.Left) == ResizeFlags.Left)
                {
                    newRect.x = command.Value.x;
                }
                if ((command.ResizeFlags & ResizeFlags.Top) == ResizeFlags.Top)
                {
                    newRect.y = command.Value.y;
                }
                if ((command.ResizeFlags & ResizeFlags.Width) == ResizeFlags.Width)
                {
                    newRect.width = command.Value.width;
                }
                if ((command.ResizeFlags & ResizeFlags.Height) == ResizeFlags.Height)
                {
                    newRect.height = command.Value.height;
                }
                placematModel.PositionAndSize = newRect;
            }
            graphToolState.MarkChanged(command.Models);
        }
    }

    public class SetPlacematCollapsedCommand : Command
    {
        public readonly IPlacematModel PlacematModel;
        public readonly bool Collapse;
        public readonly IReadOnlyList<IGraphElementModel> CollapsedElements;

        public SetPlacematCollapsedCommand()
        {
            UndoString = "Collapse Or Expand Placemat";
        }

        public SetPlacematCollapsedCommand(IPlacematModel placematModel, bool collapse,
                                           IReadOnlyList<IGraphElementModel> collapsedElements) : this()
        {
            PlacematModel = placematModel;
            Collapse = collapse;
            CollapsedElements = collapsedElements;

            UndoString = Collapse ? "Collapse Placemat" : "Expand Placemat";
        }

        public static void DefaultCommandHandler(GraphToolState graphToolState, SetPlacematCollapsedCommand command)
        {
            graphToolState.PushUndo(command);

            command.PlacematModel.Collapsed = command.Collapse;
            command.PlacematModel.HiddenElements = command.PlacematModel.Collapsed ? command.CollapsedElements : null;

            graphToolState.MarkChanged(command.PlacematModel);
        }
    }
}
