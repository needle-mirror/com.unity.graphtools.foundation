using System;
using System.Collections.Generic;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Model
{
    public interface IPortNode : IGTFNodeModel
    {
        IEnumerable<IGTFPortModel> Ports { get; }
        // PF: Add PortsById and PortsByDisplayOrder?

        // PF: these should probably be removed.
        void OnConnection(IGTFPortModel selfConnectedPortModel, IGTFPortModel otherConnectedPortModel);
        void OnDisconnection(IGTFPortModel selfConnectedPortModel, IGTFPortModel otherConnectedPortModel);
        PortCapacity GetPortCapacity(IGTFPortModel portModel);

        IGTFPortModel GetPortFitToConnectTo(IGTFPortModel portModel);
    }

    public interface IInOutPortsNode : IPortNode
    {
        IReadOnlyDictionary<string, IGTFPortModel> InputsById { get; }
        IReadOnlyDictionary<string, IGTFPortModel> OutputsById { get; }
        IReadOnlyList<IGTFPortModel> InputsByDisplayOrder { get; }
        IReadOnlyList<IGTFPortModel> OutputsByDisplayOrder { get; }
    }

    public interface ISingleInputPortNode : IInOutPortsNode
    {
        IGTFPortModel InputPort { get; }
    }

    public interface ISingleOutputPortNode : IInOutPortsNode
    {
        IGTFPortModel OutputPort { get; }
    }
}
