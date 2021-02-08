using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class CreateOppositePortalCommand : ModelCommand<IEdgePortalModel>
    {
        const string k_UndoStringSingular = "Create Opposite Portal";
        const string k_UndoStringPlural = "Create Opposite Portals";

        public CreateOppositePortalCommand()
            : base(k_UndoStringSingular) {}

        public CreateOppositePortalCommand(IReadOnlyList<IEdgePortalModel> portalModels)
            : base(k_UndoStringSingular, k_UndoStringPlural, portalModels) {}

        public static void DefaultCommandHandler(GraphToolState graphToolState, CreateOppositePortalCommand command)
        {
            if (command.Models == null)
                return;

            var portalsToOpen = command.Models.Where(p => p.CanCreateOppositePortal()).ToList();
            if (!portalsToOpen.Any())
                return;

            graphToolState.PushUndo(command);

            foreach (var portalModel in portalsToOpen)
            {
                var newPortal = graphToolState.GraphModel.CreateOppositePortal(portalModel);
                graphToolState.MarkNew(newPortal);
            }
        }
    }
}
