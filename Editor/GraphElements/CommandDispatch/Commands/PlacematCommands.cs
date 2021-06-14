using System.Collections.Generic;
using System.Linq;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Command to create a placemat.
    /// </summary>
    public class CreatePlacematCommand : UndoableCommand
    {
        /// <summary>
        /// The position and size of the new placemat.
        /// </summary>
        public Rect Position;
        /// <summary>
        /// The placemat title.
        /// </summary>
        public string Title;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreatePlacematCommand"/> class.
        /// </summary>
        public CreatePlacematCommand()
        {
            UndoString = "Create Placemat";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CreatePlacematCommand"/> class.
        /// </summary>
        /// <param name="position">The position of the new placemat.</param>
        /// <param name="title">The title of the new placemat.</param>
        public CreatePlacematCommand(Rect position, string title = null) : this()
        {
            Position = position;
            Title = title;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="graphToolState">The state.</param>
        /// <param name="command">The command.</param>
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

    /// <summary>
    /// Command to change the Z order of placemats.
    /// </summary>
    public class ChangePlacematZOrdersCommand : ModelCommand<IPlacematModel, IReadOnlyList<int>>
    {
        const string k_UndoStringSingular = "Reorder Placemat";
        const string k_UndoStringPlural = "Reorder Placemats";

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangePlacematZOrdersCommand"/> class.
        /// </summary>
        public ChangePlacematZOrdersCommand() : base(k_UndoStringSingular) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangePlacematZOrdersCommand"/> class.
        /// </summary>
        /// <param name="zOrders">The new Z order values for each of the placemats <paramref name="placematModels"/>.</param>
        /// <param name="placematModels">The placemats for which to change the Z value.</param>
        public ChangePlacematZOrdersCommand(IReadOnlyList<int> zOrders, IReadOnlyList<IPlacematModel> placematModels)
            : base(k_UndoStringSingular, k_UndoStringPlural, zOrders, placematModels)
        {
            if (zOrders == null || placematModels == null)
                return;

            Assert.AreEqual(zOrders.Count, placematModels.Count,
                "You need to provide the same number of zOrder as placemats.");
            Assert.AreEqual(zOrders.Count, zOrders.Distinct().Count(),
                "Each order should be unique in the provided list.");
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="graphToolState">The state.</param>
        /// <param name="command">The command.</param>
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

    /// <summary>
    /// Command to collapse or expand placemats.
    /// </summary>
    public class CollapsePlacematCommand : UndoableCommand
    {
        /// <summary>
        /// The placemat to collapse or expand.
        /// </summary>
        public readonly IPlacematModel PlacematModel;
        /// <summary>
        /// True if the placemat should be collapsed, false otherwise.
        /// </summary>
        public readonly bool Collapse;
        /// <summary>
        /// If collapsing the placemat, the elements hidden by the collapsed placemat.
        /// </summary>
        public readonly IReadOnlyList<IGraphElementModel> CollapsedElements;

        /// <summary>
        /// Initializes a new instance of the <see cref="CollapsePlacematCommand"/> class.
        /// </summary>
        public CollapsePlacematCommand()
        {
            UndoString = "Collapse Or Expand Placemat";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CollapsePlacematCommand"/> class.
        /// </summary>
        /// <param name="placematModel">The placemat to collapse or expand.</param>
        /// <param name="collapse">True if the placemat should be collapsed, false otherwise.</param>
        /// <param name="collapsedElements">If collapsing the placemat, the elements hidden by the collapsed placemat.</param>
        public CollapsePlacematCommand(IPlacematModel placematModel, bool collapse,
            IReadOnlyList<IGraphElementModel> collapsedElements) : this()
        {
            PlacematModel = placematModel;
            Collapse = collapse;
            CollapsedElements = collapsedElements;

            UndoString = Collapse ? "Collapse Placemat" : "Expand Placemat";
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="graphToolState">The state.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(GraphToolState graphToolState, CollapsePlacematCommand command)
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
