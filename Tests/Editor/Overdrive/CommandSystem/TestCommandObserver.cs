using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.CommandSystem
{
    class TestCommandObserver
    {
        public int CommandObserved { get; private set; }

        public void Observe(Command command)
        {
            CommandObserved++;
        }
    }
}
