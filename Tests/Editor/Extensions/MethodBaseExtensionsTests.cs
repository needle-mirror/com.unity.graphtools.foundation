using System;
using JetBrains.Annotations;
using NUnit.Framework;
using UnityEditor.EditorCommon.Extensions;

namespace UnityEditor.VisualScriptingTests.Extensions
{
    class MethodBaseExtensionsTests
    {
        [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
        class FakeObject
        {
            [UsedImplicitly]
            public FakeObject() {}
            public int FakeMethod(string a, float b) { return 0; }
            public static void StaticFakeMethod() {}
        }

        [Test]
        public void TestGetMethodDetails()
        {
            var t = typeof(FakeObject);

            var c = t.GetConstructors()[0].GetMethodDetails();
            Assert.AreEqual("Fake Object", c.ClassName);
            Assert.AreEqual("New Fake Object ()", c.MethodName);
            Assert.AreEqual("Create Fake Object", c.Details);

            var fm = t.GetMethod("FakeMethod").GetMethodDetails();
            Assert.AreEqual("Fake Object", fm.ClassName);
            Assert.AreEqual("Fake Method (String, Float)", fm.MethodName);
            Assert.AreEqual("Fake Object.Fake Method (String a, Float b) => Integer", fm.Details);

            var sfm = t.GetMethod("StaticFakeMethod").GetMethodDetails();
            Assert.AreEqual("Fake Object", sfm.ClassName);
            Assert.AreEqual("Static Fake Method ()", sfm.MethodName);
            Assert.AreEqual("Static Fake Object.Static Fake Method ()", sfm.Details);
        }
    }
}
