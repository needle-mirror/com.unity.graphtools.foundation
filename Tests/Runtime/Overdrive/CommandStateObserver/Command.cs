using System;
using NUnit.Framework;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEngine.GraphToolsFoundation.Overdrive.Tests.CommandSystem
{
    class PassThroughCommand : UndoableCommand
    {
        public static void PassThrough(TestGraphToolState graphToolState, PassThroughCommand command)
        {
            Assert.That(command, Is.Not.Null);
        }
    }

    class ChangeFooCommand : UndoableCommand
    {
        public int Value { get; }

        public ChangeFooCommand(int value)
        {
            Value = value;
        }

        public static void DefaultHandler(TestGraphToolState graphToolState, ChangeFooCommand command)
        {
            Assert.That(command, Is.Not.Null);
            using (var updater = graphToolState.FooBarStateComponent.UpdateScope)
                updater.Foo = command.Value;
        }
    }

    class ChangeBarCommand : UndoableCommand
    {
        public int Value { get; }

        public ChangeBarCommand(int value)
        {
            Value = value;
        }

        public static void DefaultHandler(TestGraphToolState graphToolState, ChangeBarCommand command)
        {
            Assert.That(command, Is.Not.Null);
            using (var updater = graphToolState.FooBarStateComponent.UpdateScope)
                updater.Bar = command.Value;
        }
    }

    class ChangeFewCommand : UndoableCommand
    {
        public int Value { get; }

        public ChangeFewCommand(int value)
        {
            Value = value;
        }

        public static void DefaultHandler(TestGraphToolState graphToolState, ChangeFewCommand command)
        {
            Assert.That(command, Is.Not.Null);
            using (var updater = graphToolState.FewBawStateComponent.UpdateScope)
                updater.Few = command.Value;
        }
    }

    class UnregisteredCommand : UndoableCommand
    { }
}
