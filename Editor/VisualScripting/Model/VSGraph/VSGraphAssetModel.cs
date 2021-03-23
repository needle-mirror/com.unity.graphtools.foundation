using System;
using JetBrains.Annotations;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEditor.VisualScripting.Model.Compilation;
using UnityEngine;

namespace UnityEditor.VisualScripting.Model
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
