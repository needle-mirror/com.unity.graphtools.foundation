using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.VisualScripting.Model.Translators;
using UnityEngine;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.VisualScriptingTests.Roslyn
{
    public class TypeSyntaxFactoryTests
    {
        [Test]
        public void Test_Plain_Type()
        {
            //Arrange
            var typeToTest = typeof(MonoBehaviour);

            //Act
            var syntaxType = typeToTest.ToTypeSyntax();

            //Assert
            Assert.That(syntaxType.ToString(), Is.EqualTo("UnityEngine.MonoBehaviour"));
        }

        [Test]
        public void Test_Single_Generic_Type()
        {
            //Arrange
            var typeToTest = typeof(List<int>);

            //Act
            var syntaxType = typeToTest.ToTypeSyntax();

            //Assert
            Assert.That(syntaxType.ToString(), Is.EqualTo("List<int>"));
        }

        [Test]
        public void Test_Nested_Generics_Type()
        {
            //Arrange
            var typeToTest = typeof(List<List<int>>);

            //Act
            var syntaxType = typeToTest.ToTypeSyntax();

            //Assert
            Assert.That(syntaxType.ToString(), Is.EqualTo("List<List<int>>"));
        }

        [Test]
        public void Test_Double_Generic_Type()
        {
            //Arrange
            var typeToTest = typeof(Dictionary<int, float>);

            //Act
            var syntaxType = typeToTest.ToTypeSyntax();

            //Assert
            Assert.That(syntaxType.ToString(), Is.EqualTo("Dictionary<int,float>"));
        }

        [Test]
        public void Test_Array_Type()
        {
            //Arrange
            var typeToTest = typeof(int[]);

            //Act
            var syntaxType = typeToTest.ToTypeSyntax();

            //Assert
            Assert.That(syntaxType.ToString(), Is.EqualTo("int[]"));
        }

        [Test]
        public void Test_Array_With_Generic_Type()
        {
            //Arrange
            var typeToTest = typeof(List<int>[]);

            //Act
            var syntaxType = typeToTest.ToTypeSyntax();

            //Assert
            Assert.That(syntaxType.ToString(), Is.EqualTo("List<int>[]"));
        }
    }
}
