using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class CreateOppositePortalAction : ModelAction<IEdgePortalModel>
    {
        const string k_UndoStringSingular = "Create Opposite Portal";
        const string k_UndoStringPlural = "Create Opposite Portals";

        public CreateOppositePortalAction()
            : base(k_UndoStringSingular) {}

        public CreateOppositePortalAction(IReadOnlyList<IEdgePortalModel> portalModels)
            : base(k_UndoStringSingular, k_UndoStringPlural, portalModels) {}

        public static void DefaultReducer(State state, CreateOppositePortalAction action)
        {
            if (action.Models == null)
                return;

            var portalsToOpen = action.Models.Where(p => p.CanCreateOppositePortal()).ToList();
            if (!portalsToOpen.Any())
                return;

            state.PushUndo(action);

            foreach (var portalModel in portalsToOpen)
            {
                var newPortal = state.GraphModel.CreateOppositePortal(portalModel);
                state.MarkNew(newPortal);
            }
        }
    }
}
