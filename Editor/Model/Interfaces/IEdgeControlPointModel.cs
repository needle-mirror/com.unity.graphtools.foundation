using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface for edge control points.
    /// </summary>
    public interface IEdgeControlPointModel
    {
        /// <summary>
        /// The position of the control point.
        /// </summary>
        Vector2 Position { get; set; }

        /// <summary>
        /// The tightness of the control point: how quickly the curve changes direction around the control point.
        /// </summary>
        float Tightness { get; set; }
    }
}
