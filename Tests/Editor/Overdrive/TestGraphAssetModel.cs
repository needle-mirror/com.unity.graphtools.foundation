using System;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    [Serializable]
    class TestGraphAssetModel : GraphAssetModel
    {
        protected override Type GraphModelType => typeof(TestGraphModel);
    }
}
