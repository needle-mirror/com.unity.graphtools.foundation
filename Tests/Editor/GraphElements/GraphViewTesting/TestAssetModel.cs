using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using Type = System.Type;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class TestGraphModel : GraphModel
    {
    }

    class TestAssetModel : GraphAssetModel
    {
        protected override Type GraphModelType => typeof(TestGraphModel);
    }
}
