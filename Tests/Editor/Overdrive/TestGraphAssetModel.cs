using System;
using System.IO;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    [Serializable]
    class TestGraphAssetModel : GraphAssetModel
    {
        static readonly string k_AssemblyRelativePath = Path.Combine("Assets", "Runtime", "Tests");
        protected override Type GraphModelType => typeof(TestGraphModel);
        public override IBlackboardGraphModel BlackboardGraphModel { get; }

        public TestGraphAssetModel()
        {
            BlackboardGraphModel = new BlackboardGraphModel { AssetModel = this };
        }
    }
}
