using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Redux
{
    class MockState : IDisposable
    {
        public int Foo { get; set; }
        public int Bar { get; set; }

        public MockState(int init)
        {
            Foo = init;
            Bar = init;
        }

        public void Dispose()
        {
        }
    }
}
