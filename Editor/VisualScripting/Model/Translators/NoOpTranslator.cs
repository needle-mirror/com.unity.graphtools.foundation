using System;
using UnityEngine.VisualScripting;

namespace UnityEditor.VisualScripting.Model.Translators
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
