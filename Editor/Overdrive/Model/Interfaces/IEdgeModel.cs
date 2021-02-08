using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public enum PortMigrationResult
    {
        None,
        PlaceholderNotNeeded,
        PlaceholderPortAdded,
        PlaceholderPortFailure,
    }

    /// <summary>
    /// Interface for a model that represents an edge in a graph.
    /// </summary>
    public interface IEdgeModel : IGraphElementModel
    {
        IPortModel FromPort { get; set; }
        IPortModel ToPort { get; set; }
        void SetPorts(IPortModel toPortModel, IPortModel fromPortModel);
        void ResetPorts();

        string FromPortId { get; }
        string ToPortId { get; }

        /// <summary>
        /// The unique identifier of the output node of the edge.
        /// </summary>
        SerializableGUID ToNodeGuid { get; }

        /// <summary>
        /// The unique identifier of the input node of the edge.
        /// </summary>
        SerializableGUID FromNodeGuid { get; }

        string EdgeLabel { get; set; }

        (PortMigrationResult, PortMigrationResult) TryMigratePorts(out INodeModel inputNode, out INodeModel outputNode);
    }
}
