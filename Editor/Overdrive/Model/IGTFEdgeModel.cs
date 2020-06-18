using System.Collections.ObjectModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Model
{
    public interface IGTFEdgeModel : IGTFGraphElementModel, ISelectable, IDeletable, IPositioned, ICopiable
    {
        IGTFPortModel FromPort { get; }
        IGTFPortModel ToPort { get; }
        ReadOnlyCollection<EdgeControlPointModel> EdgeControlPoints { get; }
        void InsertEdgeControlPoint(int atIndex, Vector2 point, float tightness);
        void ModifyEdgeControlPoint(int index, Vector2 point, float tightness);
        void RemoveEdgeControlPoint(int index);
        bool EditMode { get; set; }
        string EdgeLabel { get; set; }
    }
}
