using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIFromModelTests
{
    public class EdgeModel : IGTFEdgeModel
    {
        public IGTFGraphModel GraphModel { get; set; }
        public bool IsDeletable => true;
        public IGTFPortModel FromPort { get; }
        public IGTFPortModel ToPort { get; }
        static ReadOnlyCollection<EdgeControlPointModel> s_EdgeControlPoints = new List<EdgeControlPointModel>().AsReadOnly();
        public ReadOnlyCollection<EdgeControlPointModel> EdgeControlPoints => s_EdgeControlPoints;

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
