using System;
using System.Collections.Generic;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.Compilation;

namespace UnityEditor.VisualScripting.Model.Compilation
{
    public interface IBuilder
    {
        void Build(IEnumerable<GraphAssetModel> vsGraphAssetModels,
            Action<string, CompilerMessage[]> roslynCompilationOnBuildFinished);
    }
}
