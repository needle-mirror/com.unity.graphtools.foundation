using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.CommandSystem
{
    class PassThroughCommand : Command
    {}

    class ChangeFooCommand : Command
    {
        public int Value { get; }

        public ChangeFooCommand(int value)
        {
            Value = value;
        }
    }

    class ChangeBarCommand : Command
    {
        public int Value { get; }

        public ChangeBarCommand(int value)
        {
            Value = value;
        }
    }

    class UnregisteredCommand : Command
    {}
}
