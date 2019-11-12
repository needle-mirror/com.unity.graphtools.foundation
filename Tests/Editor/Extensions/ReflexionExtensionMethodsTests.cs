using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.VisualScripting.Editor.ConstantEditor;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.Model.Translators;
using UnityEngine;

namespace UnityEditor.VisualScriptingTests.Extensions
{
    internal class ReflexionExtensionMethodsTests
    {
        [Test]
        public void TestConstantEditorExtensionMethods()
        {
            TestExtensionMethodsAreSameFastAndSlow(mode =>
                ModelUtility.ExtensionMethodCache<IConstantEditorBuilder>.FindMatchingExtensionMethods(ConstantEditorBuilder.FilterMethods, ConstantEditorBuilder.KeySelector, mode));
        }

        [Test]
        public void TestUINodeBuilderExtensionMethods()
        {
            TestExtensionMethodsAreSameFastAndSlow(mode =>
                ModelUtility.ExtensionMethodCache<INodeBuilder>.FindMatchingExtensionMethods(NodeBuilder.FilterMethods, NodeBuilder.KeySelector, mode));
        }

        [Test]
        public void TestRoslynBuilderExtensionMethods()
        {
            TestExtensionMethodsAreSameFastAndSlow(mode =>
                ModelUtility.ExtensionMethodCache<RoslynTranslator>.FindMatchingExtensionMethods(RoslynTranslator.FilterMethods, RoslynTranslator.KeySelector, mode));
        }

        static void TestExtensionMethodsAreSameFastAndSlow(Func<ModelUtility.VisitMode, Dictionary<Type, MethodInfo>> getMethodsForMode)
        {
            var foundMethodsSlow = getMethodsForMode(ModelUtility.VisitMode.EveryMethod);
            var foundMethodsFast = getMethodsForMode(ModelUtility.VisitMode.OnlyClassesWithAttribute);
            foreach (var kp in foundMethodsSlow)
            {
                var k = kp.Key;
                var v = kp.Value;
                Assert.That(foundMethodsFast.ContainsKey(k), Is.True, $"Fast Methods doesn't contain {k.FullName}");
                Assert.That(foundMethodsFast[k], Is.EqualTo(v));
            }
            Assert.That(foundMethodsSlow.Count, Is.EqualTo(foundMethodsFast.Count));
        }
    }
}
