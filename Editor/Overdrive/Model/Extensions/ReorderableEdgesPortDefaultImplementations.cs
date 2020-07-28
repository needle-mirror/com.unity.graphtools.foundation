using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Model
{
    public static class ReorderableEdgesPortDefaultImplementations
    {
        public static void MoveEdgeFirst(IReorderableEdgesPort self, IGTFEdgeModel edge)
        {
            if (!self.HasReorderableEdges)
                return;

            self.GraphModel.MoveEdgeBefore(edge, self.GetConnectedEdges().First());
        }

        public static void MoveEdgeUp(IReorderableEdgesPort self, IGTFEdgeModel edge)
        {
            if (!self.HasReorderableEdges)
                return;

            var edges = self.GetConnectedEdges().ToList();
            var idx = edges.IndexOf(edge);
            if (idx >= 1)
                self.GraphModel.MoveEdgeBefore(edge, edges[idx - 1]);
        }

        public static void MoveEdgeDown(IReorderableEdgesPort self, IGTFEdgeModel edge)
        {
            if (!self.HasReorderableEdges)
                return;

            var edges = self.GetConnectedEdges().ToList();
            var idx = edges.IndexOf(edge);
            if (idx < edges.Count - 1)
                self.GraphModel.MoveEdgeAfter(edge, edges[idx + 1]);
        }

        public static void MoveEdgeLast(IReorderableEdgesPort self, IGTFEdgeModel edge)
        {
            if (!self.HasReorderableEdges)
                return;

            self.GraphModel.MoveEdgeAfter(edge, self.GetConnectedEdges().Last());
        }
    }
}
