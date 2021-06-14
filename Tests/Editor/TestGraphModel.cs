using System;
using GraphModel = UnityEditor.GraphToolsFoundation.Overdrive.BasicModel.GraphModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    [Serializable]
    class TestGraphModel : GraphModel
    {
        public override Type DefaultStencilType => typeof(ClassStencil);
    }

    [Serializable]
    class OtherTestGraphModel : GraphModel
    {
        public override Type DefaultStencilType => typeof(ClassStencil);
    }
}
