using System;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting;
using UnityEngine;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Models
{
    class VariableDeclarationModelTests
    {
        [TestCase("foo", "foo", "Foo")]
        [TestCase("fOo", "fOo", "F Oo")]
        [TestCase(" _foo_ ", "foo", "Foo")]
        [TestCase("foo$#@", "foo", "Foo")]
        [TestCase("!@#$%^&*()-_=+[{]}`~|;:<>,./?foo", "foo", "Foo")]
        [TestCase("foo bar", "fooBar", "Foo Bar")]
        [TestCase("foo_bar", "fooBar", "Foo Bar")]
        [TestCase("class", "myClass", "My Class")]
        [TestCase("123", "my123", "My 123")]
        [TestCase("bar", "bar1", "Bar 1", Ignore = "UniqueName generator has been temporarily disabled")]
        [TestCase("    ", "originalName", "Original Name")]
        [TestCase(" __ ", "originalName", "Original Name")]
        [TestCase("VeryWeird Name", "veryWeirdName", "Very Weird Name")]
        public void SetNameFromUserNameTest(string userInput, string expectedName, string expectedTitle)
        {
            VSGraphAssetModel graphAssetModel = (VSGraphAssetModel)GraphAssetModel.Create("test", "", typeof(VSGraphAssetModel));
            VSGraphModel graph = graphAssetModel.CreateVSGraph<ClassStencil>("test");

            var variable = graph.CreateGraphVariableDeclaration("originalName", TypeHandle.Float, true);
            variable.SetNameFromUserName(userInput);
            Assert.That(variable.VariableName, Is.EqualTo(expectedName));
            Assert.That(variable.Title, Is.EqualTo(expectedTitle));
        }

        [Test]
        public void CloningAVariableClonesFields()
        {
            VSGraphAssetModel graphAssetModel = (VSGraphAssetModel)GraphAssetModel.Create("test", "", typeof(VSGraphAssetModel));
            VSGraphModel graph = graphAssetModel.CreateVSGraph<ClassStencil>("test");
            var decl = graph.CreateGraphVariableDeclaration("asd", TypeHandle.Float, true);
            decl.Tooltip = "asdasd";
            var clone = ((VariableDeclarationModel)decl).Clone();
            Assert.IsFalse(ReferenceEquals(decl, clone));
            Assert.AreEqual(decl.Tooltip, clone.Tooltip);
            Assert.AreNotEqual(decl.GetId(), clone.GetId());
        }
    }
}
