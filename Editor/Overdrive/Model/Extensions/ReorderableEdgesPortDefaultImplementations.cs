using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public static class ReorderableEdgesPortDefaultImplementations
    {
        public static void MoveEdgeFirst(IReorderableEdgesPortModel self, IEdgeModel edge)
        {
            if (!self.HasReorderableEdges)
                return;

            self.GraphModel.MoveAfter(new[] { edge }, null);
        }

        public static void MoveEdgeUp(IReorderableEdgesPortModel self, IEdgeModel edge)
        {
            if (!self.HasReorderableEdges)
                return;

            var edges = self.GetConnectedEdges().ToList();
            var idx = edges.IndexOf(edge);
            if (idx >= 1)
                self.GraphModel.MoveBefore(new[] { edge }, edges[idx - 1]);
        }

        public static void MoveEdgeDown(IReorderableEdgesPortModel self, IEdgeModel edge)
        {
            if (!self.HasReorderableEdges)
                return;

            var edges = self.GetConnectedEdges().ToList();
            var idx = edges.IndexOf(edge);
            if (idx < edges.Count - 1)
                self.GraphModel.MoveAfter(new[] { edge }, edges[idx + 1]);
        }

        public static void MoveEdgeLast(IReorderableEdgesPortModel self, IEdgeModel edge)
        {
            if (!self.HasReorderableEdges)
                return;

            self.GraphModel.MoveBefore(new[] { edge }, null);
        }

        /// <summary>
        /// Get the order of the edge on the port.
        /// </summary>
        /// <param name="self">The port from which the edge ir originating.</param>
        /// <param name="edge">The edge for with to get the order.</param>
        /// <returns>The edge order.</returns>
        public static int GetEdgeOrder(IReorderableEdgesPortModel self, IEdgeModel edge)
        {
            var edges = self.GetConnectedEdges().ToList();
            return edges.IndexOf(edge);
        }
    }
}
