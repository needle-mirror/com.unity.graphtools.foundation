using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
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
