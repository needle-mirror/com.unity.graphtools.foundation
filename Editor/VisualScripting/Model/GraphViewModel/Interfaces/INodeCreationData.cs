using UnityEngine;

namespace UnityEditor.VisualScripting.GraphViewModel
{
    /// <summary>
    /// Interface describing data necessary for node creation.
    /// </summary>
    public interface INodeCreationData
    {
        /// <summary>
        /// The flags specifying how the node is to be spawned.
        /// </summary>
        SpawnFlags SpawnFlags { get; }

        /// <summary>
        /// The SerializableGUID to assign to the newly created item. Can be <code>null</code>.
        /// </summary>
        GUID? Guid { get; }
    }

    public interface IGraphNodeCreationData : INodeCreationData
    {
        IGraphModel GraphModel { get; }
        Vector2 Position { get; }
    }

    public interface IStackedNodeCreationData : INodeCreationData
    {
        IStackModel StackModel { get; }
        int Index { get; }
    }
}
