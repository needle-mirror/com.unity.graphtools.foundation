using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class CompilationResultModel : ICompilationResultModel
    {
        public CompilationResult lastResult;

        public CompilationResult GetLastResult()
        {
            return lastResult;
        }
    }
}
