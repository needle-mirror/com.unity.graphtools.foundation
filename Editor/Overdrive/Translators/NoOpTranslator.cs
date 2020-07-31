using System;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;

namespace UnityEngine.GraphToolsFoundation.Overdrive
{
    public class NoOpTranslator : ITranslator
    {
        public bool SupportsCompilation() => false;
        public CompilationResult TranslateAndCompile(IGTFGraphModel graphModel)
        {
            throw new NotImplementedException();
        }
    }
}
