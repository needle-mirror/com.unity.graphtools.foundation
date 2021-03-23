using System;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using NUnit.Framework;
using UnityEditor.VisualScripting.Model;
using UnityEngine;

namespace UnityEditor.VisualScriptingTests.Types
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    sealed class FakeBinaryOverload
    {
        public static FakeBinaryOverload operator +(FakeBinaryOverload a, FakeBinaryOverload b)
        {
            return a;
        }
    }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    sealed class FakeUnaryOverload
    {
        public static bool operator !(FakeUnaryOverload a)
        {
            return true;
        }
    }

    sealed class FakeNoOverload { }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    static class FakeNoOverloadExtension
    {
        public static void Ext1(this FakeNoOverload o) { }
        public static void Ext2(this FakeNoOverload[] o) { }
    }

    sealed class TypeSystemTests
    {
        [TestCase(typeof(bool))]
        [TestCase(typeof(string))]
        [TestCase(typeof(float))]
        [TestCase(typeof(int))]
        [TestCase(typeof(FakeBinaryOverload))]
        public void GetOverloadedBinaryOperators(Type type)
        {
            Assert.IsTrue(TypeSystem.GetOverloadedBinaryOperators(type).Any());
        }

        [Test]
        public void TestGetOverloadedBinaryOperators_GetKind()
        {
            Assert.AreEqual(TypeSystem.GetOverloadedBinaryOperators(typeof(FakeBinaryOverload)).First(),
                BinaryOperatorKind.Add);
        }

        [TestCase(typeof(FakeNoOverload))]
        [TestCase(typeof(FakeUnaryOverload))]
        public void TestGetOverloadedBinaryOperators_NoOverload(Type type)
        {
            Assert.IsFalse(TypeSystem.GetOverloadedBinaryOperators(type).Any());
        }

        [TestCase(typeof(bool))]
        [TestCase(typeof(float))]
        [TestCase(typeof(int))]
        [TestCase(typeof(FakeUnaryOverload))]
        public void GetOverloadedUnaryOperators(Type type)
        {
            Assert.IsTrue(TypeSystem.GetOverloadedUnaryOperators(type).Any());
        }

        [Test]
        public void TestGetOverloadedUnaryOperators_GetKind()
        {
            Assert.AreEqual(TypeSystem.GetOverloadedUnaryOperators(typeof(FakeUnaryOverload)).First(),
                UnaryOperatorKind.LogicalNot);
        }

        [TestCase(typeof(FakeNoOverload))]
        [TestCase(typeof(FakeBinaryOverload))]
        [TestCase(typeof(string))]
        public void TestGetOverloadedUnaryOperators_NoOverload(Type type)
        {
            Assert.IsFalse(TypeSystem.GetOverloadedUnaryOperators(type).Any());
        }

        [Test]
        public void TestGetExtensionMethods()
        {
            Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a =>
                a.FullName.StartsWith("Unity.GraphTools.Foundation.Editor.Tests", StringComparison.Ordinal));
            var extensions = TypeSystem.GetExtensionMethods(new[] { assembly });

            Assert.IsTrue(extensions.TryGetValue(typeof(FakeNoOverload), out var methods));
            Assert.AreEqual(2, methods.Count);
            Assert.AreEqual("Ext1", methods[0].Name);
            Assert.AreEqual("Ext2", methods[1].Name);
        }

        [Test]
        public void TestGetExtensionMethods_NoResult()
        {
            Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a =>
                a.FullName.StartsWith("nunit", StringComparison.Ordinal));
            var extensions = TypeSystem.GetExtensionMethods(new[] { assembly });

            Assert.IsFalse(extensions.TryGetValue(typeof(FakeNoOverload), out _));
        }

        [TestCase(typeof(float), typeof(int), BinaryOperatorKind.Multiply, true)]
        [TestCase(typeof(float), typeof(Vector2), BinaryOperatorKind.Multiply, true)]
        [TestCase(typeof(Vector2), typeof(float), BinaryOperatorKind.Multiply, true)]
        [TestCase(typeof(Vector2), typeof(int), BinaryOperatorKind.Multiply, true)]
        [TestCase(typeof(int), typeof(Vector2), BinaryOperatorKind.Multiply, true)]
        [TestCase(typeof(Vector2), typeof(int), BinaryOperatorKind.Add, false)]
        [TestCase(typeof(Vector2), typeof(float), BinaryOperatorKind.Divide, true)]
        [TestCase(typeof(float), typeof(Vector2), BinaryOperatorKind.Divide, false)]
        [TestCase(typeof(Vector2), typeof(Vector2), BinaryOperatorKind.Add, true)]
        [TestCase(typeof(Vector2), typeof(Vector2), BinaryOperatorKind.Modulo, false)]
        [TestCase(typeof(float), typeof(string), BinaryOperatorKind.Add, false)]
        [TestCase(typeof(Vector2), typeof(bool), BinaryOperatorKind.Multiply, false)]
        [TestCase(typeof(Vector2), typeof(KeyCode), BinaryOperatorKind.Divide, false)]
        [TestCase(typeof(KeyCode), typeof(KeyCode), BinaryOperatorKind.Equals, true)]
        [TestCase(typeof(KeyCode), typeof(KeyCode), BinaryOperatorKind.BitwiseAnd, true)]
        public void TestIsBinaryOperationPossible(Type a, Type b, BinaryOperatorKind kind, bool result)
        {
            Assert.AreEqual(result, TypeSystem.IsBinaryOperationPossible(a, b, kind));
        }
    }
}
