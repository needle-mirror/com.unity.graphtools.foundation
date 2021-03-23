using System;
using UnityEditor.EditorCommon.Redux;

namespace UnityEditor.VisualScriptingTests.Redux
{
    class PassThroughAction : object, IAction
    {
    }

    class ChangeFooAction : object, IAction
    {
        public int Value { get; }

        public ChangeFooAction(int value)
        {
            Value = value;
        }
    }

    class ChangeBarAction : object, IAction
    {
        public int Value { get; }

        public ChangeBarAction(int value)
        {
            Value = value;
        }
    }

    class UnregisteredAction : object, IAction
    {
    }
}
