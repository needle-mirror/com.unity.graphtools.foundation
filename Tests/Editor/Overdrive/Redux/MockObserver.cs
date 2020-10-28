using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Redux
{
    class MockObserver
    {
        public int ActionObserved { get; private set; }

        public void Observe(BaseAction action)
        {
            ActionObserved++;
        }
    }
}
