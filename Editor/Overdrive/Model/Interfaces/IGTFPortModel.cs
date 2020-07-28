using System;
using System.Collections.Generic;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Model
{
    public enum PortCapacity
    {
        Single,
        Multi
    }

    // ReSharper disable once InconsistentNaming
    public interface IGTFPortModel : IGTFGraphElementModel
    {
        IPortNode NodeModel { get; }
        Direction Direction { get; }
        PortType PortType { get; }
        Orientation Orientation { get; }
        PortCapacity Capacity { get; }
        Type PortDataType { get; }
        TypeHandle DataTypeHandle { get; }
        string ToolTip { get; }

        IEnumerable<IGTFPortModel> GetConnectedPorts();
        IEnumerable<IGTFEdgeModel> GetConnectedEdges();
        bool IsConnectedTo(IGTFPortModel toPort);

        PortCapacity GetDefaultCapacity();
        IConstant EmbeddedValue { get; }
        bool DisableEmbeddedValueEditor { get; }

        string UniqueName { get; }
    }
}
