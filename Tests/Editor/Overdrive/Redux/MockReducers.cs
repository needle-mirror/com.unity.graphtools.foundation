using System;
using NUnit.Framework;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Redux
{
    static class MockReducers
    {
        internal static void PassThrough(MockState previousState, PassThroughAction action)
        {
            Assert.That(action, Is.Not.Null);
        }

        internal static void ReplaceFoo(MockState previousState, ChangeFooAction action)
        {
            Assert.That(action, Is.Not.Null);
            previousState.Foo = action.Value;
        }

        internal static void ReplaceBar(MockState previousState, ChangeBarAction action)
        {
            Assert.That(action, Is.Not.Null);
            previousState.Bar = action.Value;
        }
    }
}
