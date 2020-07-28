using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Model
{
    public static class NodeModelDefaultImplementations
    {
        public static IEnumerable<IGTFEdgeModel> GetConnectedEdges(IPortNode self)
        {
            var graphModel = self.GraphModel;
            if (graphModel != null)
                return self.Ports.SelectMany(p => graphModel.GetEdgesConnections(p));

            return Enumerable.Empty<IGTFEdgeModel>();
        }

        public static IGTFPortModel GetInputPort(ISingleInputPortNode self)
        {
            return self.InputsById.Values.FirstOrDefault();
        }

        public static IGTFPortModel GetOutputPort(ISingleOutputPortNode self)
        {
            return self.OutputsById.Values.FirstOrDefault();
        }
    }
}
