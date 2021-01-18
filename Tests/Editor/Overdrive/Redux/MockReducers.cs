using System;
using NUnit.Framework;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Redux
{
    static class MockReducers
    {
        internal static void PassThrough(MockState state, PassThroughAction action)
        {
            Assert.That(action, Is.Not.Null);
        }

        internal static void ReplaceFoo(MockState state, ChangeFooAction action)
        {
            Assert.That(action, Is.Not.Null);
            state.Foo = action.Value;
        }

        internal static void ReplaceBar(MockState state, ChangeBarAction action)
        {
            Assert.That(action, Is.Not.Null);
            state.Bar = action.Value;
        }
    }
}
