using System;
using System.IO;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    [Serializable]
    class TestGraphModel : GraphModel
    {
        static readonly string k_AssemblyRelativePath = Path.Combine("Assets", "Runtime", "Tests");

        public override string GetSourceFilePath()
        {
            return Path.Combine(k_AssemblyRelativePath, TypeName + ".asset");
        }

        public override CompilationResult Compile(UnityEngine.GraphToolsFoundation.Overdrive.ITranslator translator)
        {
            return new CompilationResult();
        }
    }
}
