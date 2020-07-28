using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Model
{
    public interface IReorderableEdgesPort : IGTFPortModel
    {
        bool HasReorderableEdges { get; }

        void MoveEdgeFirst(IGTFEdgeModel edge);
        void MoveEdgeUp(IGTFEdgeModel edge);
        void MoveEdgeDown(IGTFEdgeModel edge);
        void MoveEdgeLast(IGTFEdgeModel edge);
    }
}
