using UnityEditor.GraphToolsFoundation.Overdrive;

namespace UnityEngine.GraphToolsFoundation.Overdrive
{
    public interface ITranslator
    {
        bool SupportsCompilation();
        CompilationResult Compile(IGraphModel graphModel);
        CompilationResult TranslateAndCompile(IGraphModel graphModel);
    }
}
