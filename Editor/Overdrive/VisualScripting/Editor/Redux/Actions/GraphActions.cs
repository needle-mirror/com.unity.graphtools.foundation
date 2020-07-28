using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public class ResetElementColorAction : IAction
    {
        public readonly IReadOnlyCollection<NodeModel> NodeModels;
        public readonly IReadOnlyCollection<PlacematModel> PlacematModels;

        public ResetElementColorAction(
            IReadOnlyCollection<NodeModel> nodeModels,
            IReadOnlyCollection<PlacematModel> placematModels)
        {
            NodeModels = nodeModels;
            PlacematModels = placematModels;
        }
    }

    public class ChangeElementColorAction : IAction
    {
        public readonly IReadOnlyCollection<NodeModel> NodeModels;
        public readonly IReadOnlyCollection<PlacematModel> PlacematModels;
        public readonly Color Color;

        public ChangeElementColorAction(Color color,
                                        IReadOnlyCollection<NodeModel> nodeModels,
                                        IReadOnlyCollection<PlacematModel> placematModels)
        {
            NodeModels = nodeModels;
            PlacematModels = placematModels;
            Color = color;
        }
    }
}
