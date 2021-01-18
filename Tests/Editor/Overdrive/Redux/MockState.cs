using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Redux
{
    class MockState : State
    {
        public int Foo { get; set; }
        public int Bar { get; set; }

        public MockState(int init) : base(default, null)
        {
            Foo = init;
            Bar = init;
        }

        ~MockState() => Dispose(false);
    }
}
