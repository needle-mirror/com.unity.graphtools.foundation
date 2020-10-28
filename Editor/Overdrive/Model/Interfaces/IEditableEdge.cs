using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface IEditableEdge : IEdgeModel, IMovable
    {
        IReadOnlyCollection<IEdgeControlPointModel> EdgeControlPoints { get; }
        void InsertEdgeControlPoint(int atIndex, Vector2 point, float tightness);
        void ModifyEdgeControlPoint(int index, Vector2 point, float tightness);
        void RemoveEdgeControlPoint(int index);
        bool EditMode { get; set; }
    }
}
