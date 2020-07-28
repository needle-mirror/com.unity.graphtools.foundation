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
        public IGTFPortModel PortModel;
        public IGTFEdgeModel[] EdgesToDelete;
        public Vector2 Position;
        public GraphNodeModelSearcherItem SelectedItem;

        public CreateNodeFromInputPortAction()
        {
        }

        public CreateNodeFromInputPortAction(IGTFPortModel portModel, Vector2 position,
                                             GraphNodeModelSearcherItem selectedItem, IEnumerable<IGTFEdgeModel> edgesToDelete = null)
        {
            PortModel = portModel;
            Position = position;
            SelectedItem = selectedItem;
            EdgesToDelete = edgesToDelete?.ToArray() ?? new IGTFEdgeModel[0];
        }
    }

    public class CreateNodeFromOutputPortAction : IAction
    {
        public IGTFPortModel PortModel;
        public Vector2 Position;
        public GraphNodeModelSearcherItem SelectedItem;
        public IGTFEdgeModel[] EdgesToDelete;

        public CreateNodeFromOutputPortAction()
        {
        }

        public CreateNodeFromOutputPortAction(IGTFPortModel portModel, Vector2 position,
                                              GraphNodeModelSearcherItem selectedItem, IEnumerable<IGTFEdgeModel> edgesToDelete = null)
        {
            PortModel = portModel;
            Position = position;
            SelectedItem = selectedItem;
            EdgesToDelete = edgesToDelete?.ToArray() ?? new IGTFEdgeModel[0];
        }
    }

    public class SplitEdgeAndInsertNodeAction : IAction
    {
        public readonly IGTFEdgeModel EdgeModel;
        public readonly IInOutPortsNode NodeModel;

        public SplitEdgeAndInsertNodeAction(IGTFEdgeModel edgeModel, IInOutPortsNode nodeModel)
        {
            EdgeModel = edgeModel;
            NodeModel = nodeModel;
        }
    }

    public class CreateNodeOnEdgeAction : IAction
    {
        public IGTFEdgeModel EdgeModel;
        public Vector2 Position;
        public GraphNodeModelSearcherItem SelectedItem;
        public GUID? Guid;

        public CreateNodeOnEdgeAction()
        {
        }

        public CreateNodeOnEdgeAction(IGTFEdgeModel edgeModel, Vector2 position,
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
        public (IGTFEdgeModel edge, Vector2 startPortPos, Vector2 endPortPos)[] EdgeData;

        public ConvertEdgesToPortalsAction()
        {
        }

        public ConvertEdgesToPortalsAction(IReadOnlyCollection<(IGTFEdgeModel, Vector2, Vector2)> edgeData)
        {
            EdgeData = edgeData?.ToArray();
        }
    }
}
