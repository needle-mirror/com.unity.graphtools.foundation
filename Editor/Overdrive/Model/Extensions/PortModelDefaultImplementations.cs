using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Model
{
    public static class PortModelDefaultImplementations
    {
        public static IEnumerable<IGTFPortModel> GetConnectedPorts(IGTFPortModel self)
        {
            return self?.GraphModel?.GetConnections(self) ?? Enumerable.Empty<IGTFPortModel>();
        }

        public static IEnumerable<IGTFEdgeModel> GetConnectedEdges(IGTFPortModel self)
        {
            return self?.GraphModel?.EdgeModels.Where(e => e.ToPort == self || e.FromPort == self) ??
                Enumerable.Empty<IGTFEdgeModel>();
        }

        public static bool IsConnectedTo(IGTFPortModel self, IGTFPortModel toPort)
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
