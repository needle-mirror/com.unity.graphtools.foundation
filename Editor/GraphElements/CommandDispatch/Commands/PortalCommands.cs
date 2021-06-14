using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Command to create a portal that complements an existing portal.
    /// </summary>
    public class CreateOppositePortalCommand : ModelCommand<IEdgePortalModel>
    {
        const string k_UndoStringSingular = "Create Opposite Portal";
        const string k_UndoStringPlural = "Create Opposite Portals";

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateOppositePortalCommand"/> class.
        /// </summary>
        public CreateOppositePortalCommand()
            : base(k_UndoStringSingular) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateOppositePortalCommand"/> class.
        /// </summary>
        /// <param name="portalModels">The portals for which an opposite portal should be created.</param>
        public CreateOppositePortalCommand(IReadOnlyList<IEdgePortalModel> portalModels)
            : base(k_UndoStringSingular, k_UndoStringPlural, portalModels) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateOppositePortalCommand"/> class.
        /// </summary>
        /// <param name="portalModels">The portals for which an opposite portal should be created.</param>
        public CreateOppositePortalCommand(params IEdgePortalModel[] portalModels)
            : this((IReadOnlyList<IEdgePortalModel>)portalModels) { }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="graphToolState">The state.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(GraphToolState graphToolState, CreateOppositePortalCommand command)
        {
            if (command.Models == null)
                return;

            var portalsToOpen = command.Models.Where(p => p.CanCreateOppositePortal()).ToList();
            if (!portalsToOpen.Any())
                return;

            graphToolState.PushUndo(command);

            using (var graphUpdater = graphToolState.GraphViewState.UpdateScope)
            {
                foreach (var portalModel in portalsToOpen)
                {
                    var newPortal = graphToolState.GraphViewState.GraphModel.CreateOppositePortal(portalModel);
                    graphUpdater.MarkNew(newPortal);
                }
            }
        }
    }
}
