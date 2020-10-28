using System;
using System.Collections.Generic;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface IPortNode : INodeModel
    {
        IEnumerable<IPortModel> Ports { get; }
        // PF: Add PortsById and PortsByDisplayOrder?

        // PF: these should probably be removed.
        void OnConnection(IPortModel selfConnectedPortModel, IPortModel otherConnectedPortModel);
        void OnDisconnection(IPortModel selfConnectedPortModel, IPortModel otherConnectedPortModel);
        PortCapacity GetPortCapacity(IPortModel portModel);

        IPortModel GetPortFitToConnectTo(IPortModel portModel);
    }

    public interface IInOutPortsNode : IPortNode
    {
        IReadOnlyDictionary<string, IPortModel> InputsById { get; }
        IReadOnlyDictionary<string, IPortModel> OutputsById { get; }
        IReadOnlyList<IPortModel> InputsByDisplayOrder { get; }
        IReadOnlyList<IPortModel> OutputsByDisplayOrder { get; }
    }

    public interface ISingleInputPortNode : IInOutPortsNode
    {
        IPortModel InputPort { get; }
    }

    public interface ISingleOutputPortNode : IInOutPortsNode
    {
        IPortModel OutputPort { get; }
    }
}
