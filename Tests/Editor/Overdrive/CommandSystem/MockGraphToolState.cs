using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.CommandSystem
{
    class MockGraphToolState : GraphToolState
    {
        public int Foo { get; set; }
        public int Bar { get; set; }

        public MockGraphToolState(int init) : base(default, null)
        {
            Foo = init;
            Bar = init;
        }

        ~MockGraphToolState() => Dispose(false);
    }
}
