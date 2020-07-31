using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIFromModelTests
{
    public class GhostEdgeModel : EdgeModel, IGhostEdge
    {
        public GhostEdgeModel(IGTFPortModel to, IGTFPortModel from)
            : base(to, from) {}

        public Vector2 EndPoint => Vector2.zero;
    }
}
