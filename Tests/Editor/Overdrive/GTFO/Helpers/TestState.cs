using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.Helpers
{
    public class TestState : State
    {
        IGTFGraphModel m_GraphModel;
        public override IGTFGraphModel CurrentGraphModel => m_GraphModel;

        public TestState(IGTFGraphModel graphModel) : base(null)
        {
            m_GraphModel = graphModel;
        }
    }
}
