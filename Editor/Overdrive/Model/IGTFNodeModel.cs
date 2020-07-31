using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Model
{
    public interface IGTFNodeModel : IGTFGraphElementModel, ISelectable, IPositioned, IDeletable, IDroppable, ICopiable, IDestroyable
    {
        Color Color { get; }
        bool HasUserColor { get; }
        bool HasProgress { get; }
        string IconTypeString { get; }
        ModelState State { get; }
        IReadOnlyDictionary<string, IGTFPortModel> InputsById { get; }
        IReadOnlyDictionary<string, IGTFPortModel> OutputsById { get; }
        IReadOnlyList<IGTFPortModel> InputsByDisplayOrder { get; }
        IReadOnlyList<IGTFPortModel> OutputsByDisplayOrder { get; }

        void DefineNode();

        // PF: these should probably be removed.
        void OnConnection(IGTFPortModel selfConnectedPortModel, IGTFPortModel otherConnectedPortModel);
        void OnDisconnection(IGTFPortModel selfConnectedPortModel, IGTFPortModel otherConnectedPortModel);
        PortCapacity GetPortCapacity(IGTFPortModel portModel);

        IGTFPortModel GetPortFitToConnectTo(IGTFPortModel portModel);
    }

    // ReSharper disable once InconsistentNaming
    public static class INodeModelExtensions
    {
        public static IEnumerable<IGTFPortModel> GetPortModels(this IGTFNodeModel node)
        {
            return node.InputsByDisplayOrder.Concat(node.OutputsByDisplayOrder);
        }

        public static IEnumerable<IGTFEdgeModel> GetConnectedEdges(this IGTFNodeModel nodeModel)
        {
            var graphModel = nodeModel.GraphModel;
            return nodeModel.GetPortModels().SelectMany(p => graphModel.GetEdgesConnections(p));
        }
    }
}
