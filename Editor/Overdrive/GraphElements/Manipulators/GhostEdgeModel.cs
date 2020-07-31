using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    public class GhostEdgeModel : IGTFEdgeModel, IGhostEdge
    {
        Vector2 m_Position;

        public GhostEdgeModel(IGTFGraphModel graphModel)
        {
            GraphModel = graphModel;
        }

        public IGTFPortModel FromPort { get; set; }
        public IGTFPortModel ToPort { get; set; }

        static ReadOnlyCollection<EdgeControlPointModel> s_EdgeControlPoints = new List<EdgeControlPointModel>().AsReadOnly();
        public ReadOnlyCollection<EdgeControlPointModel> EdgeControlPoints => s_EdgeControlPoints;
        public void InsertEdgeControlPoint(int atIndex, Vector2 point, float tightness)
        {
        }

        public void ModifyEdgeControlPoint(int index, Vector2 point, float tightness)
        {
        }

        public void RemoveEdgeControlPoint(int index)
        {
        }

        public bool EditMode
        {
            get => false;
            set {}
        }

        public string EdgeLabel { get; set; }

        public Vector2 EndPoint { get; set; } = Vector2.zero;

        public IGTFGraphModel GraphModel { get; }

        GUID m_Guid = GUID.Generate();

        public GUID Guid => m_Guid;

        public IGTFGraphAssetModel AssetModel => GraphModel.AssetModel;

        public void AssignNewGuid()
        {
            m_Guid = GUID.Generate();
        }

        public bool IsDeletable => false;

        public Vector2 Position
        {
            get => Vector2.zero;
            set => throw new System.NotImplementedException();
        }

        public void Move(Vector2 delta)
        {
        }

        public bool IsCopiable => false;
    }
}
