using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Model
{
    internal static class IHasPortExtensions
    {
        internal static IEnumerable<IGTFPortModel> ConnectedPortsWithReorderableEdges(this IHasPorts self)
        {
            return self.Ports != null ? self.Ports.Where(p => p.HasReorderableEdges) : Enumerable.Empty<IGTFPortModel>();
        }

        public static void RevealReorderableEdgesOrder(this IHasPorts self, bool show, IGTFEdgeModel edgeToShow = null)
        {
            var outputPortsWithReorderableEdges = self.ConnectedPortsWithReorderableEdges();
            if (edgeToShow != null)
            {
                outputPortsWithReorderableEdges = outputPortsWithReorderableEdges.Where(p => p == edgeToShow.FromPort);
            }

            foreach (var portModel in outputPortsWithReorderableEdges)
            {
                ShowEdgeIndex(portModel);
            }

            void ShowEdgeIndex(IGTFPortModel portModel)
            {
                var edges = portModel.ConnectedEdges.ToList();

                for (int i = 0; i < edges.Count; i++)
                {
                    var edge = edges[i];
                    // Only show when we have more than one edge (i.e. when it's meaningful)
                    edge.EdgeLabel = show && edges.Count > 1 ? (i + 1).ToString() : "";

                    // TODO JOCE: We need a dirty system to do the equivalent of this
                    // edge.GetUI<Edge>(GraphView)?.UpdateFromModel();
                    // Until we have one, we'll need to manually update all the outgoing edges. at every callsite.
                }
            }
        }
    }
}
