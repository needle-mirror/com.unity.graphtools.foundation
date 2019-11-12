using System;
using UnityEngine;

namespace UnityEditor.VisualScripting.GraphViewModel
{
    public interface INodeCreationData
    {
        SpawnFlags SpawnFlags { get; }
        GUID? Guid { get; }
    }

    public interface IGraphNodeCreationData : INodeCreationData
    {
        IGraphModel GraphModel { get; }
        Vector2 Position { get; }
    }

    public interface IStackedNodeCreationData : INodeCreationData
    {
        IStackModel StackModel { get; }
        int Index { get; }
    }
}
