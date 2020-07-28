namespace UnityEditor.GraphToolsFoundation.Overdrive.Model
{
    public interface IGTFGraphElementModel
    {
        IGTFGraphModel GraphModel { get; }
        GUID Guid { get; set; }
        IGTFGraphAssetModel AssetModel { get; set; }
        void AssignNewGuid();
    }
}
