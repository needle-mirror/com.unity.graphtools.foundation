using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public class CreatePortalsOppositeAction : IAction
    {
        public readonly IEnumerable<IEdgePortalModel> PortalsToOpen;

        public CreatePortalsOppositeAction(IEnumerable<IEdgePortalModel> portalModels)
        {
            PortalsToOpen = portalModels;
        }
    }
}
