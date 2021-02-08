using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.CommandSystem
{
    class MockObserver
    {
        public int CommandObserved { get; private set; }

        public void Observe(Command command)
        {
            CommandObserved++;
        }
    }
}
