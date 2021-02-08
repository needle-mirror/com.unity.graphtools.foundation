using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface IGraphTemplate
    {
        Type StencilType { get; }
        void InitBasicGraph(IGraphModel graphModel);
        string GraphTypeName { get; }
        string DefaultAssetName { get; }
    }
}
