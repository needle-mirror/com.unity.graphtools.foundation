using System;

namespace UnityEditor.VisualScripting.GraphViewModel
{
    public interface IGraphAssetModel : IDisposable
    {
        string Name { get; }
        IGraphModel GraphModel { get; }

        bool IsSameAsset(IGraphAssetModel otherGraphAssetModel);
    }
}
