using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public static class ReorderableEdgesPortDefaultImplementations
    {
        public static void MoveEdgeFirst(IReorderableEdgesPort self, IEdgeModel edge)
        {
            if (!self.HasReorderableEdges)
                return;

            self.GraphModel.MoveAfter(new[] { edge }, null);
        }

        public static void MoveEdgeUp(IReorderableEdgesPort self, IEdgeModel edge)
        {
            if (!self.HasReorderableEdges)
                return;

            var edges = self.GetConnectedEdges().ToList();
            var idx = edges.IndexOf(edge);
            if (idx >= 1)
                self.GraphModel.MoveBefore(new[] { edge }, edges[idx - 1]);
        }

        public static void MoveEdgeDown(IReorderableEdgesPort self, IEdgeModel edge)
        {
            if (!self.HasReorderableEdges)
                return;

            var edges = self.GetConnectedEdges().ToList();
            var idx = edges.IndexOf(edge);
            if (idx < edges.Count - 1)
                self.GraphModel.MoveAfter(new[] { edge }, edges[idx + 1]);
        }

        public static void MoveEdgeLast(IReorderableEdgesPort self, IEdgeModel edge)
        {
            if (!self.HasReorderableEdges)
                return;

            self.GraphModel.MoveBefore(new[] { edge }, null);
        }
    }
}
