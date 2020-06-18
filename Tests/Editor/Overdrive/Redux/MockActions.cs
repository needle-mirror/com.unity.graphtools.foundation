using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Redux
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
