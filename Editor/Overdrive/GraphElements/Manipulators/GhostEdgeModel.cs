using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    public class GhostEdgeModel : IGTFEdgeModel, IGhostEdge
    {
        GUID m_Guid = GUID.Generate();

        public IGTFGraphAssetModel AssetModel
        {
            get => GraphModel.AssetModel;
            set => GraphModel.AssetModel = value;
        }

        public IGTFGraphModel GraphModel { get; }

        public IGTFPortModel FromPort { get; set; }

        public string FromPortId => FromPort?.UniqueName;

        public string ToPortId => ToPort?.UniqueName;

        public GUID FromNodeGuid => FromPort?.NodeModel?.Guid ?? default;

        public GUID ToNodeGuid => FromPort?.NodeModel?.Guid ?? default;

        public IGTFPortModel ToPort { get; set; }

        public string EdgeLabel { get; set; }

        public Vector2 EndPoint { get; set; } = Vector2.zero;

        public GUID Guid
        {
            get => m_Guid;
            set => m_Guid = value;
        }

        public bool IsDeletable => false;

        public bool IsCopiable => false;

        public GhostEdgeModel(IGTFGraphModel graphModel)
        {
            GraphModel = graphModel;
        }

        public void SetPorts(IGTFPortModel toPortModel, IGTFPortModel fromPortModel)
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
    }
}
