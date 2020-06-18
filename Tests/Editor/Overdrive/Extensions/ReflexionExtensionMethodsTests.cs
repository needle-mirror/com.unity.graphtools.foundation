using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.Translators;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Extensions
{
    class ReflexionExtensionMethodsTests
    {
        [Test]
        public void TestConstantEditorExtensionMethods()
        {
            TestExtensionMethodsAreSameFastAndSlow(mode =>
                ExtensionMethodCache<IConstantEditorBuilder>.FindMatchingExtensionMethods(ConstantEditorBuilder.FilterMethods, ConstantEditorBuilder.KeySelector, mode));
        }

        [Test]
        public void TestUINodeBuilderExtensionMethods()
        {
            TestExtensionMethodsAreSameFastAndSlow(mode =>
                ExtensionMethodCache<ElementBuilder>.FindMatchingExtensionMethods(GraphElementFactory.FilterMethods, GraphElementFactory.KeySelector, mode));
        }

        static void TestExtensionMethodsAreSameFastAndSlow(Func<ExtensionMethodCacheVisitMode, Dictionary<Type, MethodInfo>> getMethodsForMode)
        {
            var foundMethodsSlow = getMethodsForMode(ExtensionMethodCacheVisitMode.EveryMethod);
            var foundMethodsFast = getMethodsForMode(ExtensionMethodCacheVisitMode.OnlyClassesWithAttribute);
            foreach (var kp in foundMethodsSlow)
            {
                var k = kp.Key;
                var v = kp.Value;
                Assert.That(foundMethodsFast.ContainsKey(k), NUnit.Framework.Is.True, $"Fast Methods doesn't contain {k.FullName}");
                Assert.That(foundMethodsFast[k], NUnit.Framework.Is.EqualTo(v));
            }
            Assert.That(foundMethodsSlow.Count, NUnit.Framework.Is.EqualTo(foundMethodsFast.Count));
        }
    }
}
