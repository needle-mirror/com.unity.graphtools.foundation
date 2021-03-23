using UnityEditor.GraphToolsFoundation.Overdrive;

namespace UnityEngine.GraphToolsFoundation.Overdrive
{
    public interface IGraphProcessor
    {
        GraphProcessingResult ProcessGraph(IGraphModel graphModel);
    }
}
