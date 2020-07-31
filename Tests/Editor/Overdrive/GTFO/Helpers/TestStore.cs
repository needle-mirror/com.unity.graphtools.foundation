using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.Helpers
{
    public class TestStore : Overdrive.GraphElements.Store
    {
        public TestStore(TestState initialState)
            : base(initialState)
        {
        }
    }
}
