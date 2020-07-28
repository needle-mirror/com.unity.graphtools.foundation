using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements.Utilities
{
    public class BasicEdgeModel : IGTFEdgeModel
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

        static ReadOnlyCollection<EdgeControlPointModel> s_EdgeControlPoints = new List<EdgeControlPointModel>().AsReadOnly();
        public ReadOnlyCollection<EdgeControlPointModel> EdgeControlPoints => s_EdgeControlPoints;
        public void SetPorts(IGTFPortModel toPortModel, IGTFPortModel fromPortModel)
        {
            FromPort = fromPortModel;
            ToPort = toPortModel;
        }

        public void ResetPorts()
        {
        }

        public string EdgeLabel { get; set; }

        public BasicEdgeModel(IGTFPortModel to, IGTFPortModel from)
        {
            GraphModel = null;
            FromPort = from;
            ToPort = to;
        }

        public Vector2 Position
        {
            get => Vector2.zero;
            set => throw new NotImplementedException();
        }

        public void Move(Vector2 delta)
        {
        }

        public bool IsCopiable => true;
    }
}
