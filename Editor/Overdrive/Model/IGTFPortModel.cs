using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;

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
        IGTFNodeModel NodeModel { get; }
        Direction Direction { get; }
        PortType PortType { get; }
        Orientation Orientation { get; }
        PortCapacity Capacity { get; }
        PortCapacity GetDefaultCapacity();
        Type PortDataType { get; }
        TypeHandle DataTypeHandle { get; }
        bool IsConnected { get; }
        bool IsConnectedTo(IGTFPortModel port);
        IEnumerable<IGTFEdgeModel> ConnectedEdges { get; }
        bool HasReorderableEdges { get; }
        void MoveEdgeFirst(IGTFEdgeModel edge);
        void MoveEdgeUp(IGTFEdgeModel edge);
        void MoveEdgeDown(IGTFEdgeModel edge);
        void MoveEdgeLast(IGTFEdgeModel edge);
        IEnumerable<IGTFPortModel> ConnectionPortModels { get; }
        string ToolTip { get; }
        IConstant EmbeddedValue { get; }
        bool DisableEmbeddedValueEditor { get; }

        // PF: Let's try to get rid of this.
        string UniqueName { get; }
    }
}
