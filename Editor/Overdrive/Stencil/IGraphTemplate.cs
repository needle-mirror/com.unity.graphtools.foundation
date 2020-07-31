using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface IGraphTemplate
    {
        Type StencilType { get; }
        void InitBasicGraph(IGTFGraphModel graphModel);
    }

    public interface ICreatableGraphTemplate : IGraphTemplate
    {
        string GraphTypeName { get; }
        string DefaultAssetName { get; }
    }
}
