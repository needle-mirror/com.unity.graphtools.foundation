using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel
{
    public interface IGraphAssetModel : IGTFGraphAssetModel
    {
        string Name { get; }

        bool IsSameAsset(IGraphAssetModel otherGraphAssetModel);
    }
}
