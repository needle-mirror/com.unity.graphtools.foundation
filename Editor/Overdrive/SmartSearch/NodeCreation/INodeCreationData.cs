using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface IGraphNodeCreationData
    {
        SpawnFlags SpawnFlags { get; }
        IGraphModel GraphModel { get; }
        Vector2 Position { get; }
        SerializableGUID Guid { get; }
    }
}
