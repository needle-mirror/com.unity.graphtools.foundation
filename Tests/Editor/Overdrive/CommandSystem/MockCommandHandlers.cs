using System;
using NUnit.Framework;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.CommandSystem
{
    static class MockCommandHandlers
    {
        internal static void PassThrough(MockGraphToolState graphToolState, PassThroughCommand command)
        {
            Assert.That(command, Is.Not.Null);
        }

        internal static void ReplaceFoo(MockGraphToolState graphToolState, ChangeFooCommand command)
        {
            Assert.That(command, Is.Not.Null);
            graphToolState.Foo = command.Value;
        }

        internal static void ReplaceBar(MockGraphToolState graphToolState, ChangeBarCommand command)
        {
            Assert.That(command, Is.Not.Null);
            graphToolState.Bar = command.Value;
        }
    }
}
