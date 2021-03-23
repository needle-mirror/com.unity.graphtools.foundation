using System;
using JetBrains.Annotations;
using NUnit.Framework;
using UnityEditor.VisualScripting.Model;

// ReSharper disable AccessToStaticMemberViaDerivedType
namespace UnityEditor.VisualScriptingTests.Types
{
    struct CustomBool { }

#pragma warning disable 660,661
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    struct A
    {
        public static CustomBool operator ==(A lhs, A rhs) { return new CustomBool(); }
        public static CustomBool operator !=(A lhs, A rhs) { return new CustomBool(); }
        public static CustomBool operator ==(float lhs, A rhs) { return new CustomBool(); }
        public static CustomBool operator !=(float lhs, A rhs) { return new CustomBool(); }
    }
#pragma warning restore 660,661

    [System.ComponentModel.Category("BinaryOperators")]
    class VisualScriptingBinaryOperatorTests
    {
        [TestCase(BinaryOperatorKind.Equals, null, null, typeof(bool))]
        [TestCase(BinaryOperatorKind.Add, null, null, typeof(Unknown))]
        [TestCase(BinaryOperatorKind.Add, typeof(Unknown), null, typeof(Unknown))]
        [TestCase(BinaryOperatorKind.Add, null, typeof(int), typeof(int))]
        [TestCase(BinaryOperatorKind.Add, null, typeof(int), typeof(int))]
        [TestCase(BinaryOperatorKind.Add, null, typeof(float), typeof(float))]
        [TestCase(BinaryOperatorKind.Subtract, null, typeof(float), typeof(float))]
        [TestCase(BinaryOperatorKind.Subtract, typeof(float), null, typeof(float))]
        public void IncompleteInputTests(BinaryOperatorKind kind, Type x, Type y, Type expected)
        {
            Type inferredType = BinaryOperatorNodeModel.GetOutputTypeFromInputs(kind, x, y);
            Assert.That(inferredType, Is.EqualTo(expected));
        }

        [TestCase(BinaryOperatorKind.Equals, null, typeof(float))]
        [TestCase(BinaryOperatorKind.NotEqual, typeof(int), typeof(float))]
        [TestCase(BinaryOperatorKind.GreaterThan, typeof(byte), typeof(float))]
        [TestCase(BinaryOperatorKind.GreaterThanOrEqual, typeof(byte), null)]
        [TestCase(BinaryOperatorKind.LessThan, typeof(long), typeof(int))]
        [TestCase(BinaryOperatorKind.LessThanOrEqual, typeof(float), typeof(sbyte))]
        public void BooleanTypeTest(BinaryOperatorKind kind, Type x, Type y)
        {
            Type inferredType = BinaryOperatorNodeModel.GetOutputTypeFromInputs(kind, x, y);
            Assert.That(inferredType, Is.EqualTo(typeof(bool)));
        }

        [TestCase(BinaryOperatorKind.Equals, typeof(A), typeof(A), typeof(CustomBool))]
        [TestCase(BinaryOperatorKind.Equals, typeof(float), typeof(A), typeof(CustomBool))]
        [TestCase(BinaryOperatorKind.Equals, typeof(A), null, typeof(CustomBool))]
        [TestCase(BinaryOperatorKind.NotEqual, typeof(A), typeof(A), typeof(CustomBool))]
        [TestCase(BinaryOperatorKind.NotEqual, typeof(float), typeof(A), typeof(CustomBool))]
        [TestCase(BinaryOperatorKind.NotEqual, typeof(A), null, typeof(CustomBool))]
        public void CustomBooleanTypeTest(BinaryOperatorKind kind, Type x, Type y, Type expected)
        {
            Type inferredType = BinaryOperatorNodeModel.GetOutputTypeFromInputs(kind, x, y);
            Assert.That(inferredType, Is.EqualTo(expected));
        }

        [TestCase(typeof(byte), typeof(byte), typeof(byte))]
        [TestCase(typeof(byte), typeof(short), typeof(short))]
        [TestCase(typeof(byte), typeof(ushort), typeof(ushort))]
        [TestCase(typeof(byte), typeof(int), typeof(int))]
        [TestCase(typeof(byte), typeof(uint), typeof(uint))]
        [TestCase(typeof(byte), typeof(float), typeof(float))]
        [TestCase(typeof(byte), typeof(long), typeof(long))]
        [TestCase(typeof(byte), typeof(decimal), typeof(decimal))]
        [TestCase(typeof(int), typeof(byte), typeof(int))]
        [TestCase(typeof(int), typeof(decimal), typeof(decimal))]
        [TestCase(typeof(float), typeof(short), typeof(float))]
        [TestCase(typeof(float), typeof(decimal), typeof(decimal))]
        [TestCase(typeof(long), typeof(ushort), typeof(long))]
        [TestCase(typeof(byte), typeof(sbyte), typeof(short), Ignore = "Not Implemented")]
        [TestCase(typeof(short), typeof(ushort), typeof(int), Ignore = "Not Implemented")]
        [TestCase(typeof(int), typeof(uint), typeof(long), Ignore = "Not Implemented")]
        [TestCase(typeof(long), typeof(ulong), typeof(ulong), Ignore = "Not Implemented")]
        [TestCase(typeof(float), typeof(float), typeof(double), Ignore = "Not Implemented")]
        [TestCase(typeof(double), typeof(double), typeof(decimal), Ignore = "Not Implemented")]
        public void MultiplyTypeTests(Type x, Type y, Type expected)
        {
            Type inferredType = BinaryOperatorNodeModel.GetOutputTypeFromInputs(BinaryOperatorKind.Multiply, x, y);
            Assert.That(inferredType, Is.EqualTo(expected));
        }
    }
}
