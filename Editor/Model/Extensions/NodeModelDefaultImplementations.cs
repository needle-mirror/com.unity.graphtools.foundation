using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Default implementations for some node model methods.
    /// </summary>
    public static class NodeModelDefaultImplementations
    {
        public static IEnumerable<IEdgeModel> GetConnectedEdges(IPortNodeModel self)
        {
            var graphModel = self.GraphModel;
            if (graphModel != null)
                return self.Ports.SelectMany(p => graphModel.GetEdgesConnections(p));

            return Enumerable.Empty<IEdgeModel>();
        }

        public static IPortModel GetInputPort(ISingleInputPortNodeModel self)
        {
            return self.InputsById.Values.FirstOrDefault();
        }

        public static IPortModel GetOutputPort(ISingleOutputPortNodeModel self)
        {
            return self.OutputsById.Values.FirstOrDefault();
        }
    }
}
