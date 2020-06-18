using System;
using UnityEngine.GraphToolsFoundation.Overdrive.VisualScripting;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.Translators
{
    public class NoOpTranslator : ITranslator
    {
        public bool SupportsCompilation() => false;
        public CompilationResult TranslateAndCompile(VSGraphModel graphModel, AssemblyType assemblyType, CompilationOptions compilationOptions)
        {
            throw new NotImplementedException();
        }
    }
}
