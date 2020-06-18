using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIFromModelTests
{
    class GraphModel : IGTFGraphModel
    {
        List<IGTFEdgeModel> m_EdgeModels = new List<IGTFEdgeModel>();

        public void Dispose()
        {
        }

        public IReadOnlyList<IGTFEdgeModel> EdgeModels => m_EdgeModels;

        public IGTFEdgeModel CreateEdgeGTF(IGTFPortModel inputPort, IGTFPortModel outputPort)
        {
            var edge = new EdgeModel(inputPort, outputPort);
            edge.GraphModel = this;
            m_EdgeModels.Add(edge);
            return edge;
        }

        public void DeleteElements(IEnumerable<IGTFGraphElementModel> graphElementModels)
        {
        }
    }
}
