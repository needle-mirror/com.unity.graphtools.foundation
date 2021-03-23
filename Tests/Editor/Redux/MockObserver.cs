using System;
using UnityEditor.EditorCommon.Redux;

namespace UnityEditor.VisualScriptingTests.Redux
{
    class MockObserver
    {
        public int ActionObserved { get; private set; }

        public void Observe(IAction action)
        {
            ActionObserved++;
        }
    }
}
