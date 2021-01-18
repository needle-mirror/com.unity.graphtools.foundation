using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public enum RequestCompilationOptions
    {
        Default,
        SaveGraph,
    }

    public class CompilationStateComponent : AssetViewStateComponent
    {
        public CompilationResult m_LastResult;

        public bool CompilationPending { get; set; }

        public Action<RequestCompilationOptions> OnCompilationRequest;

        public void RequestCompilation(RequestCompilationOptions options)
        {
            OnCompilationRequest?.Invoke(options);
        }

        public CompilationResult GetLastResult()
        {
            return m_LastResult;
        }
    }
}
