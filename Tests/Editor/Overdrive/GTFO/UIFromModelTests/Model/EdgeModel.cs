using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIFromModelTests
{
    public class EdgeModel : IEditableEdge
    {
        public IGTFGraphModel GraphModel { get; set; }

        GUID m_GUID = GUID.Generate();
        public GUID Guid
        {
            get => m_GUID;
            set => m_GUID = value;
        }

        public IGTFGraphAssetModel AssetModel
        {
            get => GraphModel.AssetModel;
            set => GraphModel.AssetModel = value;
        }

        public void AssignNewGuid()
        {
            m_GUID = GUID.Generate();
        }

        public bool IsDeletable => true;
        public IGTFPortModel FromPort { get; set; }
        public IGTFPortModel ToPort { get; set; }

        public string FromPortId => FromPort?.UniqueName;

        public string ToPortId => ToPort?.UniqueName;

        public GUID FromNodeGuid => FromPort?.NodeModel?.Guid ?? default;

        public GUID ToNodeGuid => FromPort?.NodeModel?.Guid ?? default;


        static IReadOnlyCollection<IEdgeControlPointModel> s_EdgeControlPoints = new List<IEdgeControlPointModel>();
        public IReadOnlyCollection<IEdgeControlPointModel> EdgeControlPoints => s_EdgeControlPoints;

        public void InsertEdgeControlPoint(int atIndex, Vector2 point, float tightness)
        {
            throw new NotImplementedException();
        }

        public void ModifyEdgeControlPoint(int index, Vector2 point, float tightness)
        {
            throw new NotImplementedException();
        }

        public void RemoveEdgeControlPoint(int index)
        {
            throw new NotImplementedException();
        }

        public bool EditMode { get; set; }
        public string EdgeLabel { get; set; }

        public EdgeModel(IGTFPortModel to, IGTFPortModel from)
        {
            GraphModel = null;
            FromPort = from;
            ToPort = to;
        }

        public void SetPorts(IGTFPortModel toPortModel, IGTFPortModel fromPortModel)
        {
            FromPort = fromPortModel;
            ToPort = toPortModel;
        }

        public void ResetPorts()
        {
        }

        public Vector2 Position
        {
            get => Vector2.zero;
            set => throw new NotImplementedException();
        }

        public void Move(Vector2 delta)
        {
            throw new NotImplementedException();
        }

        public bool IsCopiable => true;
    }
}
