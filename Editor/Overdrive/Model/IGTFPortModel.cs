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
        Orientation Orientation { get; }
        PortCapacity Capacity { get; }
        Type PortDataType { get; }
        bool IsConnected { get; }
        bool IsConnectedTo(IGTFPortModel port);
        IEnumerable<IGTFEdgeModel> ConnectedEdges { get; }
        bool HasReorderableEdges { get; }
        void MoveEdgeFirst(IGTFEdgeModel edge);
        void MoveEdgeUp(IGTFEdgeModel edge);
        void MoveEdgeDown(IGTFEdgeModel edge);
        void MoveEdgeLast(IGTFEdgeModel edge);

        string ToolTip { get; }
    }
}
