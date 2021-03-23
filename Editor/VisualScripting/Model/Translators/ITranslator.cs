using System;
using UnityEngine.VisualScripting;

namespace UnityEditor.VisualScripting.Model.Translators
{
    public interface ITranslator
    {
        bool SupportsCompilation();
        CompilationResult TranslateAndCompile(VSGraphModel graphModel, AssemblyType assemblyType, CompilationOptions compilationOptions);
    }
}
