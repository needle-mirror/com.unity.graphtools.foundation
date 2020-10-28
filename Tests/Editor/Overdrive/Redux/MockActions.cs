using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Redux
{
    class PassThroughAction : BaseAction
    {}

    class ChangeFooAction : BaseAction
    {
        public int Value { get; }

        public ChangeFooAction(int value)
        {
            Value = value;
        }
    }

    class ChangeBarAction : BaseAction
    {
        public int Value { get; }

        public ChangeBarAction(int value)
        {
            Value = value;
        }
    }

    class UnregisteredAction : BaseAction
    {}
}
