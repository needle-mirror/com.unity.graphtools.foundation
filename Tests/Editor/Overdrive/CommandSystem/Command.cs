using System;
using NUnit.Framework;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.CommandSystem
{
    class PassThroughCommand : Command
    {
        public static void PassThrough(TestGraphToolState graphToolState, PassThroughCommand command)
        {
            Assert.That(command, Is.Not.Null);
        }
    }

    class ChangeFooCommand : Command
    {
        public int Value { get; }

        public ChangeFooCommand(int value)
        {
            Value = value;
        }

        public static void DefaultHandler(TestGraphToolState graphToolState, ChangeFooCommand command)
        {
            Assert.That(command, NUnit.Framework.Is.Not.Null);
            using (var updater = graphToolState.FooBarStateComponent.UpdateScope)
                updater.Foo = command.Value;
        }
    }

    class ChangeBarCommand : Command
    {
        public int Value { get; }

        public ChangeBarCommand(int value)
        {
            Value = value;
        }

        public static void DefaultHandler(TestGraphToolState graphToolState, ChangeBarCommand command)
        {
            Assert.That(command, NUnit.Framework.Is.Not.Null);
            using (var updater = graphToolState.FooBarStateComponent.UpdateScope)
                updater.Bar = command.Value;
        }
    }

    class UnregisteredCommand : Command
    { }
}
