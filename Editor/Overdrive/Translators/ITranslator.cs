using System;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;

namespace UnityEngine.GraphToolsFoundation.Overdrive
{
    public interface ITranslator
    {
        bool SupportsCompilation();
        CompilationResult TranslateAndCompile(IGTFGraphModel graphModel);
    }
}
