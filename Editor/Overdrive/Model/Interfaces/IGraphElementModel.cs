using System.Collections.Generic;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface IGraphElementModel
    {
        IGraphModel GraphModel { get; }
        GUID Guid { get; set; }
        IGraphAssetModel AssetModel { get; set; }
        void AssignNewGuid();
        IReadOnlyList<Capabilities> Capabilities { get; }
    }
}
