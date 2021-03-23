using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface IEdgeControlPointModel
    {
        Vector2 Position { get; set; }
        float Tightness { get; set; }
    }
}
