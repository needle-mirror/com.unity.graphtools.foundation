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

            using (var graphUpdater = graphToolState.GraphViewState.UpdateScope)
            {
                var placematModel = graphToolState.GraphViewState.GraphModel.CreatePlacemat(command.Position);
                if (command.Title != null)
                    placematModel.Title = command.Title;

                graphUpdater.MarkNew(placematModel);
            }
        }
    }

    public class ChangePlacematZOrdersCommand : ModelCommand<IPlacematModel, int[]>
    {
        const string k_UndoStringSingular = "Reorder Placemat";
        const string k_UndoStringPlural = "Reorder Placemats";

        public ChangePlacematZOrdersCommand() : base(k_UndoStringSingular) { }

        public ChangePlacematZOrdersCommand(int[] zOrders, IPlacematModel[] placematModels)
            : base(k_UndoStringSingular, k_UndoStringPlural, placematModels, zOrders)
        {
            if (zOrders == null || placematModels == null)
                return;

            Assert.AreEqual(zOrders.Length, placematModels.Length,
                "You need to provide the same number of zOrder as placemats.");
            Assert.AreEqual(zOrders.Count(), zOrders.Distinct().Count(),
                "Each order should be unique in the provided list.");
        }

        public static void DefaultCommandHandler(GraphToolState graphToolState, ChangePlacematZOrdersCommand command)
        {
            if (!command.Models.Any())
                return;

            graphToolState.PushUndo(command);

            using (var graphUpdater = graphToolState.GraphViewState.UpdateScope)
            {
                for (var index = 0; index < command.Models.Count; index++)
                {
                    var placematModel = command.Models[index];
                    var zOrder = command.Value[index];
                    placematModel.ZOrder = zOrder;
                }

                graphUpdater.MarkChanged(command.Models);
            }
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

            using (var graphUpdater = graphToolState.GraphViewState.UpdateScope)
            {
                command.PlacematModel.Collapsed = command.Collapse;
                command.PlacematModel.HiddenElements = command.PlacematModel.Collapsed ? command.CollapsedElements : null;

                graphUpdater.MarkChanged(command.PlacematModel);
            }
        }
    }
}
