using System;

namespace UnityEditor.VisualScripting.GraphViewModel
{
    /// <summary>
    /// Spawn flags dictates multiple operations during the NodeModels creation.
    /// </summary>
    [Flags]
    public enum SpawnFlags
    {
        None   = 0,
        /// <summary>
        /// During the NodeModel creation, it registers an undo point so the spawning/adding can be undoable/redoable
        /// </summary>
        Undoable  = 1 << 0,
        /// <summary>
        /// During the NodeModel creation, it with a SerializableAsset under it to make it serializable in the asset.
        /// </summary>
        CreateNodeAsset = 1 << 1,
        /// <summary>
        /// The created NodeModel is not added to a Stack/Graph. Useful for display only purposes.
        /// </summary>
        Orphan  = 1 << 2,
        /// <summary>
        /// This include the SpawnFlags.Orphan and SpawnFlags.CreateNodeAsset
        /// </summary>
        Default = Undoable | CreateNodeAsset,
    }

    public static class SpawnFlagsExtensions
    {
        public static bool IsOrphan(this SpawnFlags f) => (f & SpawnFlags.Orphan) != 0;
        public static bool IsUndoable(this SpawnFlags f) => (f & SpawnFlags.Undoable) != 0;
        public static bool IsSerializable(this SpawnFlags f) => (f & SpawnFlags.CreateNodeAsset) != 0;
    }
}
