using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.VisualScriptingTests.Models
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
        [TestCase("    ", "temp", "Temp")]
        [TestCase(" __ ", "temp", "Temp")]
        [TestCase("VeryWeird Name", "veryWeirdName", "Very Weird Name")]
        public void SetNameFromUserNameTest(string userInput, string expectedName, string expectedTitle)
        {
            VSGraphAssetModel graphAssetModel = (VSGraphAssetModel)GraphAssetModel.Create("test", "", typeof(VSGraphAssetModel));
            VSGraphModel graph = graphAssetModel.CreateVSGraph<ClassStencil>("test");

            var method = graph.CreateFunction("method", Vector2.left * 200);
            method.CreateFunctionVariableDeclaration("bar", typeof(int).GenerateTypeHandle(graph.Stencil));
            var variable = method.CreateFunctionVariableDeclaration("temp", typeof(int).GenerateTypeHandle(graph.Stencil));

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

        [Test]
        public void Test_FunctionVariableDeclarationsWithSameName()
        {
            VSGraphAssetModel graphAssetModel = (VSGraphAssetModel)GraphAssetModel.Create("test", "", typeof(VSGraphAssetModel));
            VSGraphModel graph = graphAssetModel.CreateVSGraph<ClassStencil>("test");

            var method = graph.CreateFunction("TestFunction", Vector2.zero);

            var declaration0 = method.CreateFunctionVariableDeclaration("var", typeof(int).GenerateTypeHandle(graph.Stencil));
            var declaration1 = method.CreateFunctionVariableDeclaration("var", typeof(int).GenerateTypeHandle(graph.Stencil));
            Assert.That(declaration0, Is.Not.EqualTo(declaration1));
            Assert.That(method.FunctionVariableModels.Count(), Is.EqualTo(2));
        }

        [Test]
        public void Test_FunctionParameterDeclarationsWithSameName()
        {
            VSGraphAssetModel graphAssetModel = (VSGraphAssetModel)GraphAssetModel.Create("test", "", typeof(VSGraphAssetModel));
            VSGraphModel graph = graphAssetModel.CreateVSGraph<ClassStencil>("test");

            var method = graph.CreateFunction("TestFunction", Vector2.zero);

            var declaration0 = method.CreateAndRegisterFunctionParameterDeclaration("param", typeof(int).GenerateTypeHandle(graph.Stencil));
            var declaration1 = method.CreateAndRegisterFunctionParameterDeclaration("param", typeof(int).GenerateTypeHandle(graph.Stencil));
            Assert.That(declaration0, Is.Not.EqualTo(declaration1));
            Assert.That(method.FunctionParameterModels.Count(), Is.EqualTo(2));
        }

        [Test]
        public void Test_FunctionVariableDeclarationsIsSerializedInGraphAsset()
        {
            VSGraphAssetModel graphAssetModel = (VSGraphAssetModel)GraphAssetModel.Create("test", "Assets/MyGraphTest.asset", typeof(VSGraphAssetModel));
            VSGraphModel graph = graphAssetModel.CreateVSGraph<ClassStencil>("test");
            FunctionModel method = graph.CreateFunction("TestFunction", Vector2.zero);

            VariableDeclarationModel declaration = method.CreateFunctionVariableDeclaration("var", typeof(int).GenerateTypeHandle(graph.Stencil));

            string nodeModelPath = AssetDatabase.GetAssetPath(declaration.InitializationModel.SerializableAsset);
            string graphAssetModelPath = AssetDatabase.GetAssetPath(graphAssetModel);
            Assert.That(nodeModelPath, Is.EqualTo(graphAssetModelPath));
            AssetDatabase.DeleteAsset(graphAssetModelPath);
        }
    }
}
