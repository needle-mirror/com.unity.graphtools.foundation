using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.Helpers
{
    public class TestState : GraphToolsFoundation.Overdrive.GraphElements.State
    {
        public override IGTFGraphModel GraphModel { get; }

        public TestState(IGTFGraphModel graphModel)
        {
            GraphModel = graphModel;
        }
    }
}
