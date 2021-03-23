using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public static class PortModelDefaultImplementations
    {
        public static IEnumerable<IPortModel> GetConnectedPorts(IPortModel self)
        {
            return self?.GraphModel?.GetConnections(self) ?? Enumerable.Empty<IPortModel>();
        }

        public static IEnumerable<IEdgeModel> GetConnectedEdges(IPortModel self)
        {
            return self?.GraphModel?.EdgeModels.Where(e => e.ToPort == self || e.FromPort == self) ??
                Enumerable.Empty<IEdgeModel>();
        }

        public static bool IsConnectedTo(IPortModel self, IPortModel toPort)
        {
            if (self.GraphModel == null)
                return false;

            var edgeModels = self.GraphModel.EdgeModels.Where(e =>
                e.ToPort == self && e.FromPort == toPort ||
                e.FromPort == self && e.ToPort == toPort);
            return edgeModels.Any();
        }
    }
}
