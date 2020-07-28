using System;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    [Serializable]
    class TestGraphAssetModel : GraphAssetModel
    {
        protected override Type GraphModelType => typeof(TestGraphModel);
    }
}
