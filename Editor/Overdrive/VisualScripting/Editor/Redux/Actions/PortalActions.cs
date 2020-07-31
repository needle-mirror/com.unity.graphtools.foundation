using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public class CreatePortalsOppositeAction : IAction
    {
        public IGTFEdgePortalModel[] PortalsToOpen;

        public CreatePortalsOppositeAction()
        {
        }

        public CreatePortalsOppositeAction(IEnumerable<IGTFEdgePortalModel> portalModels)
        {
            PortalsToOpen = portalModels?.ToArray();
        }
    }
}
