using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface IGraphTemplate
    {
        Type StencilType { get; }
        void InitBasicGraph(IGraphModel graphModel);
    }

    public interface ICreatableGraphTemplate : IGraphTemplate
    {
        string GraphTypeName { get; }
        string DefaultAssetName { get; }
    }
}
