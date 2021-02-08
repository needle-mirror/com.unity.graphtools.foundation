using System.Collections.Generic;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface for a model that represents an element in a graph.
    /// </summary>
    public interface IGraphElementModel
    {
        /// <summary>
        /// The graph model to which the element belongs.
        /// </summary>
        IGraphModel GraphModel { get; }

        /// <summary>
        /// The unique identifier of the element.
        /// </summary>
        SerializableGUID Guid { get; set; }

        /// <summary>
        /// The asset model to which the element belongs.
        /// </summary>
        IGraphAssetModel AssetModel { get; set; }

        /// <summary>
        /// Assign a newly generated GUID to the model.
        /// </summary>
        void AssignNewGuid();

        /// <summary>
        /// The list of capabilities of the element.
        /// </summary>
        IReadOnlyList<Capabilities> Capabilities { get; }
    }
}
