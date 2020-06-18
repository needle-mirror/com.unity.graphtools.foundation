using System;
using UnityEngine.GraphToolsFoundation.Overdrive.VisualScripting;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.Translators
{
    public interface ITranslator
    {
        bool SupportsCompilation();
        CompilationResult TranslateAndCompile(VSGraphModel graphModel, AssemblyType assemblyType, CompilationOptions compilationOptions);
    }
}
