using System;

namespace UnityEditor.VisualScripting.Model
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
