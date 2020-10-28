using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public static class NodeModelDefaultImplementations
    {
        public static IEnumerable<IEdgeModel> GetConnectedEdges(IPortNode self)
        {
            var graphModel = self.GraphModel;
            if (graphModel != null)
                return self.Ports.SelectMany(p => graphModel.GetEdgesConnections(p));

            return Enumerable.Empty<IEdgeModel>();
        }

        public static IPortModel GetInputPort(ISingleInputPortNode self)
        {
            return self.InputsById.Values.FirstOrDefault();
        }

        public static IPortModel GetOutputPort(ISingleOutputPortNode self)
        {
            return self.OutputsById.Values.FirstOrDefault();
        }
    }
}
