using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using Type = System.Type;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class TestGraphModel : GraphModel
    {
        public TestGraphModel()
        {
        }
    }
    class TestAssetModel : GraphAssetModel
    {
        public override IBlackboardGraphModel BlackboardGraphModel
        {
            get;
        }

        TestAssetModel()
        {
            BlackboardGraphModel = new BlackboardGraphModel();
        }

        protected override Type GraphModelType
        {
            get { return typeof(TestGraphModel); }
        }

    }
}
