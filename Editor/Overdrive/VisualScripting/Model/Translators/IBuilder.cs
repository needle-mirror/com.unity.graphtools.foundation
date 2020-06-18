using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEditor.Compilation;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.Compilation
{
    public interface IBuilder
    {
        void Build(IEnumerable<GraphAssetModel> vsGraphAssetModels,
            Action<string, CompilerMessage[]> roslynCompilationOnBuildFinished);
    }
}
