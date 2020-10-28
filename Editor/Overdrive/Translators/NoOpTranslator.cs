using System;
using UnityEditor.GraphToolsFoundation.Overdrive;

namespace UnityEngine.GraphToolsFoundation.Overdrive
{
    public class NoOpTranslator : ITranslator
    {
        public bool SupportsCompilation() => false;
        public CompilationResult TranslateAndCompile(IGraphModel graphModel)
        {
            throw new NotImplementedException();
        }
    }
}
