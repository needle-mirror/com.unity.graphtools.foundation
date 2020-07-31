using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements.Utilities
{
    public class BasicEdgeModel : IGTFEdgeModel
    {
        public IGTFGraphModel GraphModel { get; set; }

        GUID m_GUID = GUID.Generate();
        public GUID Guid => m_GUID;

        public IGTFGraphAssetModel AssetModel => GraphModel.AssetModel;

        public void AssignNewGuid()
        {
            m_GUID = GUID.Generate();
        }

        public bool IsDeletable => true;
        public IGTFPortModel FromPort { get; }
        public IGTFPortModel ToPort { get; }
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

        public bool EditMode { get; set; }
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
