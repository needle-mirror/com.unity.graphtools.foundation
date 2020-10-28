using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.InternalModels
{
    public class GhostEdgeModel : IEdgeModel, IGhostEdge
    {
        GUID m_Guid = GUID.Generate();

        public IGraphAssetModel AssetModel
        {
            get => GraphModel.AssetModel;
            set => GraphModel.AssetModel = value;
        }

        public IGraphModel GraphModel { get; }

        public IPortModel FromPort { get; set; }

        public string FromPortId => FromPort?.UniqueName;

        public string ToPortId => ToPort?.UniqueName;

        public GUID FromNodeGuid => FromPort?.NodeModel?.Guid ?? default;

        public GUID ToNodeGuid => FromPort?.NodeModel?.Guid ?? default;

        public IPortModel ToPort { get; set; }

        public string EdgeLabel { get; set; }

        public Vector2 EndPoint { get; set; } = Vector2.zero;

        public GUID Guid
        {
            get => m_Guid;
            set => m_Guid = value;
        }

        public GhostEdgeModel(IGraphModel graphModel)
        {
            GraphModel = graphModel;
        }

        public void SetPorts(IPortModel toPortModel, IPortModel fromPortModel)
        {
            FromPort = fromPortModel;
            ToPort = toPortModel;
        }

        public void ResetPorts()
        {
        }

        public void AssignNewGuid()
        {
            m_Guid = GUID.Generate();
        }

        // Ghost edges have no capabilities
        readonly List<Capabilities> m_Capabilities = new List<Capabilities> {Overdrive.Capabilities.NoCapabilities};
        public virtual IReadOnlyList<Capabilities> Capabilities => m_Capabilities;
    }
}
