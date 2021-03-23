using System;
using NUnit.Framework;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.VisualScriptingTests.Redux
{
    static class MockReducers
    {
        internal static MockState PassThrough(MockState previousState, PassThroughAction action)
        {
            Assert.That(action, Is.Not.Null);
            return previousState;
        }

        internal static MockState ReplaceFoo(MockState previousState, ChangeFooAction action)
        {
            Assert.That(action, Is.Not.Null);
            previousState.Foo = action.Value;
            return previousState;
        }

        internal static MockState ReplaceBar(MockState previousState, ChangeBarAction action)
        {
            Assert.That(action, Is.Not.Null);
            previousState.Bar = action.Value;
            return previousState;
        }
    }
}
