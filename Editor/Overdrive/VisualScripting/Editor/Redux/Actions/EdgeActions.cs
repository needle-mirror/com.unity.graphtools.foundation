using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.SmartSearch;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public class CreateNodeFromInputPortAction : IAction
    {
        public readonly IPortModel PortModel;
        public readonly IEnumerable<IGTFEdgeModel> EdgesToDelete;
        public readonly Vector2 Position;
        public readonly GraphNodeModelSearcherItem SelectedItem;

        public CreateNodeFromInputPortAction(IPortModel portModel, Vector2 position,
                                             GraphNodeModelSearcherItem selectedItem, IEnumerable<IGTFEdgeModel> edgesToDelete = null)
        {
            PortModel = portModel;
            Position = position;
            SelectedItem = selectedItem;
            EdgesToDelete = edgesToDelete ?? Enumerable.Empty<IGTFEdgeModel>();
        }
    }

    public class CreateNodeFromOutputPortAction : IAction
    {
        public readonly IPortModel PortModel;
        public readonly Vector2 Position;
        public readonly GraphNodeModelSearcherItem SelectedItem;
        public readonly IEnumerable<IGTFEdgeModel> EdgesToDelete;

        public CreateNodeFromOutputPortAction(IPortModel portModel, Vector2 position,
                                              GraphNodeModelSearcherItem selectedItem, IEnumerable<IGTFEdgeModel> edgesToDelete = null)
        {
            PortModel = portModel;
            Position = position;
            SelectedItem = selectedItem;
            EdgesToDelete = edgesToDelete ?? Enumerable.Empty<IGTFEdgeModel>();
        }
    }

    public class SplitEdgeAndInsertNodeAction : IAction
    {
        public readonly IEdgeModel EdgeModel;
        public readonly INodeModel NodeModel;

        public SplitEdgeAndInsertNodeAction(IEdgeModel edgeModel, INodeModel nodeModel)
        {
            EdgeModel = edgeModel;
            NodeModel = nodeModel;
        }
    }

    public class CreateNodeOnEdgeAction : IAction
    {
        public readonly IEdgeModel EdgeModel;
        public readonly Vector2 Position;
        public readonly GraphNodeModelSearcherItem SelectedItem;
        public readonly GUID? Guid;

        public CreateNodeOnEdgeAction(IEdgeModel edgeModel, Vector2 position,
                                      GraphNodeModelSearcherItem selectedItem, GUID? guid = null)
        {
            EdgeModel = edgeModel;
            Position = position;
            SelectedItem = selectedItem;
            Guid = guid;
        }
    }
    public class ConvertEdgesToPortalsAction : IAction
    {
        public readonly IReadOnlyCollection<(IEdgeModel edge, Vector2 startPortPos, Vector2 endPortPos)> EdgeData;

        public ConvertEdgesToPortalsAction(IReadOnlyCollection<(IEdgeModel, Vector2, Vector2)> edgeData)
        {
            EdgeData = edgeData;
        }
    }
}
