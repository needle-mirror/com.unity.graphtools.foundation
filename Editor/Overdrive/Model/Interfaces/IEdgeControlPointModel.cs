using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Model
{
    public interface IEdgeControlPointModel
    {
        Vector2 Position { get; set; }
        float Tightness { get; set; }
    }
}
