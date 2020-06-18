using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIFromModelTests
{
    class PortModel : IGTFPortModel, IHasTitle
    {
        bool m_Connected;
        public IGTFGraphModel GraphModel { get; set; }
        public IGTFNodeModel NodeModel { get; set; }
        public Direction Direction { get; set; }
        public Orientation Orientation { get; set; } = Orientation.Horizontal;
        public PortCapacity Capacity { get; set; } = PortCapacity.Single;
        public Type PortDataType { get; } = typeof(float);
        public bool IsConnected => m_Connected || ConnectedEdges.Any();

        public void FakeIsConnected(bool connected)
        {
            m_Connected = connected;
        }

        public bool IsConnectedTo(IGTFPortModel port)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IGTFEdgeModel> ConnectedEdges =>
            ((GraphModel)GraphModel)?.EdgeModels?.Where(e => e.FromPort == this || e.ToPort == this) ?? Enumerable.Empty<IGTFEdgeModel>();
        public bool HasReorderableEdges { get; set; } = false;
        public void MoveEdgeFirst(IGTFEdgeModel edge)
        {
            throw new NotImplementedException();
        }

        public void MoveEdgeUp(IGTFEdgeModel edge)
        {
            throw new NotImplementedException();
        }

        public void MoveEdgeDown(IGTFEdgeModel edge)
        {
            throw new NotImplementedException();
        }

        public void MoveEdgeLast(IGTFEdgeModel edge)
        {
            throw new NotImplementedException();
        }

        public string ToolTip { get; set; }
        public string Title { get; set; }
        public string DisplayTitle => Title;
    }
}
