using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Model
{
    public interface IEditableEdge : IGTFEdgeModel, IPositioned
    {
        IReadOnlyCollection<IEdgeControlPointModel> EdgeControlPoints { get; }
        void InsertEdgeControlPoint(int atIndex, Vector2 point, float tightness);
        void ModifyEdgeControlPoint(int index, Vector2 point, float tightness);
        void RemoveEdgeControlPoint(int index);
        bool EditMode { get; set; }
    }
}
