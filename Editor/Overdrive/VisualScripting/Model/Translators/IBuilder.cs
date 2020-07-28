using System;
using System.Collections.Generic;
using UnityEditor.Compilation;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.Compilation
{
    public interface IBuilder
    {
        void Build(IEnumerable<GraphAssetModel> vsGraphAssetModels,
            Action<string, CompilerMessage[]> roslynCompilationOnBuildFinished);
    }
}
