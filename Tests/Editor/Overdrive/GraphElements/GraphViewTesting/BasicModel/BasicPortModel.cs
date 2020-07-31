using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements.Utilities
{
    public class BasicPortModel : IGTFPortModel
    {
        public IGTFGraphModel GraphModel { get; }

        GUID m_GUID = GUID.Generate();
        public GUID Guid => m_GUID;
        public IGTFGraphAssetModel AssetModel => GraphModel.AssetModel;

        public void AssignNewGuid()
        {
            m_GUID = GUID.Generate();
        }

        public IGTFNodeModel NodeModel { get; }
        public Direction Direction { get; }
        public PortType PortType => PortType.Data;
        public Orientation Orientation { get; }
        public PortCapacity Capacity { get; }
        public PortCapacity GetDefaultCapacity()
        {
            return Direction == Direction.Input ? PortCapacity.Single : PortCapacity.Multi;
        }

        public Type PortDataType { get; }
        public bool IsConnected => ConnectedEdges.Any();
        public bool IsConnectedTo(IGTFPortModel port)
        {
            if (GraphModel is BasicGraphModel bgm)
                return bgm.Edges.Any(e =>
                    (e.FromPort == this && e.ToPort == port) || (e.FromPort == port && e.ToPort == this));

            return false;
        }

        public IEnumerable<IGTFEdgeModel> ConnectedEdges
        {
            get { return (GraphModel as BasicGraphModel)?.Edges.Where(e => e.FromPort == this || e.ToPort == this); }
        }

        public bool HasReorderableEdges => false;
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

        public string ToolTip => "";
        public IConstant EmbeddedValue => null;
        public bool DisableEmbeddedValueEditor => false;
        public string UniqueName => m_GUID.ToString();
        public IEnumerable<IGTFPortModel> ConnectionPortModels => Enumerable.Empty<IGTFPortModel>();
        public TypeHandle DataTypeHandle { get; } = TypeHandle.Int;

        public BasicPortModel(IGTFNodeModel nodeModel, Direction direction, Orientation orientation, PortCapacity capacity, Type type = null)
        {
            GraphModel = nodeModel.GraphModel;
            NodeModel = nodeModel;
            Direction = direction;
            Orientation = orientation;
            Capacity = capacity;
            PortDataType = type != null ? type : typeof(float);
        }
    }
}
