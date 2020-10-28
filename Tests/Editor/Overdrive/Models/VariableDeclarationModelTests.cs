using System;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
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
            var graphAssetModel = IGraphAssetModelHelper.Create("test", "", typeof(TestGraphAssetModel));
            graphAssetModel.CreateGraph("test", typeof(ClassStencil));

            var variable = graphAssetModel.GraphModel.CreateGraphVariableDeclaration("originalName", TypeHandle.Float, ModifierFlags.None, true);
            (variable as IRenamable)?.Rename(userInput);
            Assert.That(variable.VariableName, Is.EqualTo(expectedName));
            Assert.That(variable.DisplayTitle, Is.EqualTo(expectedTitle));
        }

        [Test]
        public void CloningAVariableClonesFields()
        {
            var graphAssetModel = IGraphAssetModelHelper.Create("test", "", typeof(TestGraphAssetModel));
            graphAssetModel.CreateGraph("test", typeof(ClassStencil));

            var decl = graphAssetModel.GraphModel.CreateGraphVariableDeclaration("asd", TypeHandle.Float, ModifierFlags.None, true);
            decl.Tooltip = "asdasd";
            var clone = (decl as VariableDeclarationModel).Clone();
            Assert.IsFalse(ReferenceEquals(decl, clone));
            Assert.AreEqual(decl.Tooltip, clone.Tooltip);
            Assert.AreNotEqual(decl.Guid, clone.Guid);
        }
    }
}
