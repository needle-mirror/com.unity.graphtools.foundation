using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIFromModelTests
{
    class PortModel : IReorderableEdgesPort, IHasTitle
    {
        bool m_Connected;
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

        public IPortNode NodeModel { get; set; }
        public Direction Direction { get; set; }
        public PortType PortType => PortType.Data;
        public Orientation Orientation { get; set; } = Orientation.Horizontal;
        public PortCapacity Capacity { get; set; } = PortCapacity.Single;

        public PortCapacity GetDefaultCapacity()
        {
            return Direction == Direction.Input ? PortCapacity.Single : PortCapacity.Multi;
        }

        public Type PortDataType { get; } = typeof(float);

        public bool HasReorderableEdges { get; set; }

        public string ToolTip { get; set; }

        public virtual IEnumerable<IGTFPortModel> GetConnectedPorts()
        {
            return PortModelDefaultImplementations.GetConnectedPorts(this);
        }

        public virtual IEnumerable<IGTFEdgeModel> GetConnectedEdges()
        {
            return PortModelDefaultImplementations.GetConnectedEdges(this);
        }

        public virtual bool IsConnectedTo(IGTFPortModel toPort)
        {
            return PortModelDefaultImplementations.IsConnectedTo(this, toPort);
        }

        public virtual void MoveEdgeFirst(IGTFEdgeModel edge)
        {
            ReorderableEdgesPortDefaultImplementations.MoveEdgeFirst(this, edge);
        }

        public virtual void MoveEdgeUp(IGTFEdgeModel edge)
        {
            ReorderableEdgesPortDefaultImplementations.MoveEdgeUp(this, edge);
        }

        public virtual void MoveEdgeDown(IGTFEdgeModel edge)
        {
            ReorderableEdgesPortDefaultImplementations.MoveEdgeDown(this, edge);
        }

        public virtual void MoveEdgeLast(IGTFEdgeModel edge)
        {
            ReorderableEdgesPortDefaultImplementations.MoveEdgeLast(this, edge);
        }

        public IConstant EmbeddedValue => null;
        public bool DisableEmbeddedValueEditor => false;
        public string UniqueName => m_GUID.ToString();
        public TypeHandle DataTypeHandle { get; } = TypeHandle.Int;
        public string Title { get; set; }
        public string DisplayTitle => Title;
    }
}
