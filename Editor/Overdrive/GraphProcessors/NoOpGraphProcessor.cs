using System;
using UnityEditor.GraphToolsFoundation.Overdrive;

namespace UnityEngine.GraphToolsFoundation.Overdrive
{
    public class NoOpGraphProcessor : IGraphProcessor
    {
        public GraphProcessingResult ProcessGraph(IGraphModel graphModel)
        {
            return new GraphProcessingResult();
        }
    }
}
