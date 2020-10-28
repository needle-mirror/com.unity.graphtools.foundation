using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO
{
    public class TestState : State
    {
        IGraphModel m_GraphModel;
        public override IGraphModel CurrentGraphModel => m_GraphModel;

        public TestState(IGraphModel graphModel) : base(null)
        {
            m_GraphModel = graphModel;
        }
    }
}
