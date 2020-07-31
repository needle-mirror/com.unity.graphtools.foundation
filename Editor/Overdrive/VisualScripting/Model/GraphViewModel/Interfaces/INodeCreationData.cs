using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel
{
    public interface IGraphNodeCreationData
    {
        SpawnFlags SpawnFlags { get; }
        IGTFGraphModel GraphModel { get; }
        Vector2 Position { get; }
        GUID Guid { get; }
    }
}
