using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.Helpers
{
    public class TestStore : GraphToolsFoundation.Overdrive.GraphElements.Store<TestState>
    {
        public TestStore(TestState initialState)
            : base(initialState)
        {
        }
    }
}
