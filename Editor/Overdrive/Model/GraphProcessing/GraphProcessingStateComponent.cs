using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public enum RequestGraphProcessingOptions
    {
        Default,
        SaveGraph,
    }

    public class GraphProcessingStateComponent : AssetViewStateComponent
    {
        public GraphProcessingResult m_LastResult;

        public bool GraphProcessingPending { get; set; }

        public Action<RequestGraphProcessingOptions> OnGraphProcessingRequest;

        public void RequestGraphProcessing(RequestGraphProcessingOptions options)
        {
            OnGraphProcessingRequest?.Invoke(options);
        }

        public GraphProcessingResult GetLastResult()
        {
            return m_LastResult;
        }
    }
}
