using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.InternalModels
{
    /// <summary>
    /// A model that represents an edge in a graph.
    /// </summary>
    /// <remarks>
    /// A ghost edge is usually used as an edge that shows where an edge would connect to during edge
    /// connection manipulations.
    /// </remarks>
    public class GhostEdgeModel : GraphElementModel, IEdgeModel, IGhostEdge
    {
        /// <inheritdoc />
        public override IGraphModel GraphModel { get; }

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

        public GhostEdgeModel(IGraphModel graphModel)
        {
            GraphModel = graphModel;
            m_AssetModel = graphModel?.AssetModel as GraphAssetModel;
        }

        public void SetPorts(IPortModel toPortModel, IPortModel fromPortModel)
        {
            FromPort = fromPortModel;
            ToPort = toPortModel;
        }

        public (PortMigrationResult, PortMigrationResult) TryMigratePorts(out INodeModel inputNode, out INodeModel outputNode)
        {
            inputNode = null;
            outputNode = null;
            return (PortMigrationResult.None, PortMigrationResult.None);
        }
    }
}
