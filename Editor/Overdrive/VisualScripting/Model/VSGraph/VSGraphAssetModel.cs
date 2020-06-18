using System;
using JetBrains.Annotations;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.Compilation;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    [PublicAPI]
    public class VSGraphAssetModel : GraphAssetModel
    {
        public virtual VSGraphModel CreateVSGraph<T>(string graphName, bool writeOnDisk = true) where T : Stencil
        {
            return CreateVSGraphUsingStencil(graphName, typeof(T), writeOnDisk);
        }

        public virtual VSGraphModel CreateVSGraphUsingStencil(string graphName, Type stencilType, bool writeOnDisk = true)
        {
            return CreateGraph<VSGraphModel>(graphName, stencilType, writeOnDisk);
        }

        public string GetClassName()
        {
            var graphModel = (VSGraphModel)GraphModel;
            return graphModel.TypeName;
        }

        public IBuilder Builder => GraphModel.Stencil.Builder;
    }
}
