using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface IReorderableEdgesPort : IPortModel
    {
        bool HasReorderableEdges { get; }

        void MoveEdgeFirst(IEdgeModel edge);
        void MoveEdgeUp(IEdgeModel edge);
        void MoveEdgeDown(IEdgeModel edge);
        void MoveEdgeLast(IEdgeModel edge);
    }
}
