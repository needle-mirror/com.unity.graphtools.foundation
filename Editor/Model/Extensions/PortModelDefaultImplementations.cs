using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Default implementations for some <see cref="IPortModel"/> methods.
    /// </summary>
    public static class PortModelDefaultImplementations
    {
        /// <summary>
        /// Gets the ports connected to this port.
        /// </summary>
        /// <param name="self">The port for which we want to get the connected ports.</param>
        /// <returns>The ports connected to this port.</returns>
        public static IEnumerable<IPortModel> GetConnectedPorts(IPortModel self)
        {
            return self?.GraphModel?.GetConnections(self) ?? Enumerable.Empty<IPortModel>();
        }

        /// <summary>
        /// Gets the edges connected to this port.
        /// </summary>
        /// <param name="self">The port for which we want to get the connected edges.</param>
        /// <returns>The edges connected to this port.</returns>
        public static IEnumerable<IEdgeModel> GetConnectedEdges(IPortModel self)
        {
            return self?.GraphModel?.EdgeModels.Where(e => e.ToPort == self || e.FromPort == self) ??
                Enumerable.Empty<IEdgeModel>();
        }

        /// <summary>
        /// Checks whether two ports are connected.
        /// </summary>
        /// <param name="self">The first port.</param>
        /// <param name="toPort">The second port.</param>
        /// <returns>True if there is at least one edge that connects the two ports.</returns>
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
