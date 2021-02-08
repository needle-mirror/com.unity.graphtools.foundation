using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.InternalModels
{
    /// <summary>
    /// A model that represents an edge in a graph.
    /// </summary>
    /// <remarks>
    /// A ghost edge is usually used as an edge that shows where an edge would connect to during edge
    /// connection manipulations.
    /// </remarks>
    public class GhostEdgeModel : IEdgeModel, IGhostEdge
    {
        SerializableGUID m_Guid = SerializableGUID.Generate();

        public IGraphAssetModel AssetModel
        {
            get => GraphModel.AssetModel;
            set => GraphModel.AssetModel = value;
        }

        public IGraphModel GraphModel { get; }

        public IPortModel FromPort { get; set; }

        public string FromPortId => FromPort?.UniqueName;

        public string ToPortId => ToPort?.UniqueName;

        /// <summary>
        /// The unique identifier of the input node of the edge.
        /// </summary>
        public SerializableGUID FromNodeGuid => FromPort?.NodeModel?.Guid ?? default;

        /// <summary>
        /// The unique identifier of the output node of the edge.
        /// </summary>
        public SerializableGUID ToNodeGuid => FromPort?.NodeModel?.Guid ?? default;

        public IPortModel ToPort { get; set; }

        public string EdgeLabel { get; set; }

        public Vector2 EndPoint { get; set; } = Vector2.zero;

        /// <summary>
        /// The unique identifier of the edge.
        /// </summary>
        public SerializableGUID Guid
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

        /// <summary>
        /// Assign a newly generated GUID to the model.
        /// </summary>
        public void AssignNewGuid()
        {
            m_Guid = SerializableGUID.Generate();
        }

        // Ghost edges have no capabilities
        readonly List<Capabilities> m_Capabilities = new List<Capabilities> { UnityEditor.GraphToolsFoundation.Overdrive.Capabilities.NoCapabilities};
        public virtual IReadOnlyList<Capabilities> Capabilities => m_Capabilities;

        public (PortMigrationResult, PortMigrationResult) TryMigratePorts(out INodeModel inputNode, out INodeModel outputNode)
        {
            inputNode = null;
            outputNode = null;
            return (PortMigrationResult.None, PortMigrationResult.None);
        }
    }
}
