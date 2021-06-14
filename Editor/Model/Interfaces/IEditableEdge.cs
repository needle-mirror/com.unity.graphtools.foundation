using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface for edges that can be routed with control points.
    /// </summary>
    public interface IEditableEdge : IEdgeModel, IMovable
    {
        /// <summary>
        /// The control points.
        /// </summary>
        IReadOnlyList<IEdgeControlPointModel> EdgeControlPoints { get; }

        /// <summary>
        /// Inserts a control point.
        /// </summary>
        /// <param name="atIndex">Where to insert the control point in the list of control points.</param>
        /// <param name="point">The location of the new control point.</param>
        /// <param name="tightness">The tightness of the new control point.</param>
        void InsertEdgeControlPoint(int atIndex, Vector2 point, float tightness);

        /// <summary>
        /// Modifies an existing control point.
        /// </summary>
        /// <param name="index">The index of the control point to modify.</param>
        /// <param name="point">The new location of the control point.</param>
        /// <param name="tightness">The new tightness of the control point.</param>
        void ModifyEdgeControlPoint(int index, Vector2 point, float tightness);

        /// <summary>
        /// Removes a control point.
        /// </summary>
        /// <param name="index">The index of the control point to remove.</param>
        void RemoveEdgeControlPoint(int index);

        /// <summary>
        /// Whether the edges is in edit mode.
        /// </summary>
        bool EditMode { get; set; }
    }
}
