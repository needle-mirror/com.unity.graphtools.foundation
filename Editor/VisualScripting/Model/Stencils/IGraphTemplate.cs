using System;
using UnityEngine;

namespace UnityEditor.VisualScripting.Model
{
    // Define a way to init a graph
    public interface IGraphTemplate
    {
        Type StencilType { get; }
        void InitBasicGraph(VSGraphModel graphModel);
    }

    // Graph templated by a GameObject
    // Define a template that can be created from anywhere
    public interface ICreatableGraphTemplate : IGraphTemplate
    {
        string GraphTypeName { get; }
        string DefaultAssetName { get; }
    }
}
