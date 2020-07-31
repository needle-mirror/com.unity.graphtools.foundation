namespace UnityEditor.GraphToolsFoundation.Overdrive.Model
{
    public interface IGTFGraphElementModel
    {
        IGTFGraphModel GraphModel { get; }
        GUID Guid { get; }
        IGTFGraphAssetModel AssetModel { get; }
        void AssignNewGuid();
    }
}
