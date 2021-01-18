using System;
using UnityEditor.GraphToolsFoundation.Overdrive;

namespace UnityEngine.GraphToolsFoundation.Overdrive
{
    public class NoOpTranslator : ITranslator
    {
        public bool SupportsCompilation() => false;

        public CompilationResult Compile(IGraphModel graphModel)
        {
            throw new NotImplementedException();
        }

        public CompilationResult TranslateAndCompile(IGraphModel graphModel)
        {
            throw new NotImplementedException();
        }
    }
}
