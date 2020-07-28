using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements.Utilities
{
    public class BasicPortModel : IGTFPortModel
    {
        public IGTFGraphModel GraphModel { get; }

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

        public IPortNode NodeModel { get; }
        public Direction Direction { get; }
        public PortType PortType => PortType.Data;
        public Orientation Orientation { get; }
        public PortCapacity Capacity { get; }
        public PortCapacity GetDefaultCapacity()
        {
            return Direction == Direction.Input ? PortCapacity.Single : PortCapacity.Multi;
        }

        public Type PortDataType { get; }

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

        public bool HasReorderableEdges => false;

        public string ToolTip => "";
        public IConstant EmbeddedValue => null;
        public bool DisableEmbeddedValueEditor => false;
        public string UniqueName => m_GUID.ToString();
        public TypeHandle DataTypeHandle { get; } = TypeHandle.Int;

        public BasicPortModel(IPortNode nodeModel, Direction direction, Orientation orientation, PortCapacity capacity, Type type = null)
        {
            GraphModel = nodeModel.GraphModel;
            NodeModel = nodeModel;
            Direction = direction;
            Orientation = orientation;
            Capacity = capacity;
            PortDataType = type != null ? type : typeof(float);
        }

        public static bool Equivalent(IGTFPortModel a, IGTFPortModel b)
        {
            if (a == null || b == null)
                return a == b;

            return a.Direction == b.Direction && a.NodeModel.Guid == b.NodeModel.Guid && a.UniqueName == b.UniqueName;
        }
    }
}
