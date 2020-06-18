using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel
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
}
