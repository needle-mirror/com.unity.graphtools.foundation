using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIFromModelTests
{
    class PortModel : IGTFPortModel, IHasTitle
    {
        bool m_Connected;
        public IGTFGraphModel GraphModel { get; set; }

        GUID m_GUID = GUID.Generate();
        public GUID Guid => m_GUID;
        public IGTFGraphAssetModel AssetModel => GraphModel.AssetModel;

        public void AssignNewGuid()
        {
            m_GUID = GUID.Generate();
        }

        public IGTFNodeModel NodeModel { get; set; }
        public Direction Direction { get; set; }
        public PortType PortType => PortType.Data;
        public Orientation Orientation { get; set; } = Orientation.Horizontal;
        public PortCapacity Capacity { get; set; } = PortCapacity.Single;
        public PortCapacity GetDefaultCapacity()
        {
            return Direction == Direction.Input ? PortCapacity.Single : PortCapacity.Multi;
        }

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
        public IConstant EmbeddedValue => null;
        public bool DisableEmbeddedValueEditor => false;
        public string UniqueName => m_GUID.ToString();
        public IEnumerable<IGTFPortModel> ConnectionPortModels => Enumerable.Empty<IGTFPortModel>();
        public TypeHandle DataTypeHandle { get; } = TypeHandle.Int;
        public string Title { get; set; }
        public string DisplayTitle => Title;
    }
}
