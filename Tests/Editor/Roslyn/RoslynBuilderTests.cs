using System;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Moq;
using NUnit.Framework;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEditor.VisualScripting.Model.Translators;
using UnityEngine;
using CompilationOptions = UnityEngine.VisualScripting.CompilationOptions;
using Debug = UnityEngine.Debug;

// ReSharper disable AccessToStaticMemberViaDerivedType
namespace UnityEditor.VisualScriptingTests.Roslyn
{
    class TestObject
    {
        public static void DoStuff() {}
    }

    class RoslynBuilderTests : BaseFixture
    {
        protected override bool CreateGraphOnStartup => true;

        [Test]
        public void Test_Translate_UsingAlias()
        {
            var b = new RoslynTranslator(Stencil);
            b.AddUsingAlias("TestAlias", "UnityEditor.VisualScriptingTests.Roslyn.TestAlias");
            var c = b.Translate(GraphModel, CompilationOptions.Default);
            var d = c.GetRoot();

            var ud = d.DescendantNodes().OfType<UsingDirectiveSyntax>().Where(n => n.Alias != null).ToList();
            Assert.That(ud.Count, Is.EqualTo(5));
            Assert.That(ud.Count(u => u.Alias.Name.Identifier.Text == "TestAlias"), Is.EqualTo(1));
        }

        [Test]
        public void Test_Translate_UsingDirective()
        {
            var type = typeof(TestObject);
            var a = GraphModel.CreateFunction("A", Vector2.zero);
            var i = typeof(TestObject).GetMethod(nameof(TestObject.DoStuff));
            a.CreateStackedNode<FunctionCallNodeModel>("Do", 0, SpawnFlags.Default, n => n.MethodInfo = i);

            var b = new RoslynTranslator(Stencil);
            var c = b.Translate(GraphModel, CompilationOptions.Default);
            var d = c.GetRoot();

            var ud = d.DescendantNodes().OfType<UsingDirectiveSyntax>();
            Assert.That(ud.Count(n => n.Name.ToString() == type.Namespace), Is.EqualTo(1));
        }

        [Test]
        public void Test_CodeAnalysisCSharpVersionIsSameAsUnity()
        {
            var unityCsVersionStr = UnityCompilerTestHelper.GetCompilerVersion();
            Assert.That(unityCsVersionStr, Is.Not.Null, "something went wrong with unity_csc");
            var couldParse = LanguageVersionFacts.TryParse(unityCsVersionStr, out var unityCsVersion);
            Assert.That(couldParse, Is.True, $"unable to parse unity C# version: '{unityCsVersionStr}'");
            var roslynTranslatorCsVersion = RoslynTranslator.LanguageVersion.MapSpecifiedToEffectiveVersion();
            Assert.That(roslynTranslatorCsVersion, Is.EqualTo(unityCsVersion));
        }

        [Test]
        public void Test_Translate_Constructor()
        {
            FunctionModel a = GraphModel.CreateFunction("A", Vector2.zero);

            // Debug.Log(...)
            MethodInfo logMethod = typeof(Debug).GetMethod(nameof(Debug.Log), new[] { typeof(object) });
            Assume.That(logMethod, Is.Not.Null);
            FunctionCallNodeModel log = a.CreateStackedNode<FunctionCallNodeModel>("Log", 0, SpawnFlags.Default, n => n.MethodInfo = logMethod);

            // new Vector4(x, y)
            ConstructorInfo ctor = typeof(Vector4).GetConstructor(new[] { typeof(float), typeof(float) });
            Assume.That(ctor, Is.Not.Null);
            FunctionCallNodeModel newV4 = GraphModel.CreateNode<FunctionCallNodeModel>("New Vector4", Vector2.left * 200, SpawnFlags.Default, n => n.MethodInfo = ctor);

            GraphModel.CreateEdge(log.GetParameterPorts().First(), newV4.OutputPort);

            var b = new RoslynTranslator(Stencil);
            var c = b.Translate(GraphModel, CompilationOptions.Default);

            SyntaxNode d = c.GetRoot();
            StatementSyntax stmt = d.DescendantNodes().OfType<MethodDeclarationSyntax>().First(n => n.Identifier.ValueText == "A")
                .Body.Statements.First();

            ExpressionSyntax arg = ((InvocationExpressionSyntax)((ExpressionStatementSyntax)stmt).Expression).ArgumentList.Arguments.Single().Expression;

            Assert.That(arg.ToFullString(), Is.EqualTo("new UnityEngine.Vector4(0F, 0F)"));
        }

        [Test]
        public void Test_Translate_SimpleResizableMethod()
        {
            GraphModel.CreateFunction("A", Vector2.zero);
            var b = new RoslynTranslator(Stencil);
            var c = b.Translate(GraphModel, CompilationOptions.Default);
            var d = c.GetRoot();

            Assert.That(d.DescendantNodes().OfType<MethodDeclarationSyntax>().Where(n => n.Identifier.ValueText == "A").ToArray().Length, Is.EqualTo(1));
            Assert.That(d.DescendantNodes().OfType<ParameterSyntax>().ToArray().Length, Is.EqualTo(0));
        }

        [Test]
        public void Test_Translate_SimpleMethod2Params()
        {
            var a = GraphModel.CreateFunction("A", Vector2.zero);
            a.CreateFunctionVariableDeclaration("l", typeof(int).GenerateTypeHandle(GraphModel.Stencil));
            a.CreateAndRegisterFunctionParameterDeclaration("a", typeof(int).GenerateTypeHandle(GraphModel.Stencil));

            var b = new RoslynTranslator(Stencil);
            var c = b.Translate(GraphModel, CompilationOptions.Default);
            var d = c.GetRoot();


            Assert.That(d.DescendantNodes().OfType<MethodDeclarationSyntax>().Where(n => n.Identifier.ValueText == "A").ToArray().Length, Is.EqualTo(1));
            Assert.That(d.DescendantNodes().OfType<ParameterSyntax>().ToArray().Length, Is.EqualTo(1));
            Assert.That(d.DescendantNodes().OfType<LocalDeclarationStatementSyntax>().ToArray().Length, Is.EqualTo(1));
        }

        [Test]
        public void Test_Translate_DetectInfiniteLoop()
        {
            var function = GraphModel.CreateFunction("Function", Vector2.zero);
            var stack0 = GraphModel.CreateStack(string.Empty, Vector2.zero);
            var stack1 = GraphModel.CreateStack(string.Empty, Vector2.zero);
            GraphModel.CreateEdge(stack0.InputPorts[0], function.OutputPort);
            GraphModel.CreateEdge(stack0.InputPorts[0], stack1.OutputPorts[0]);
            GraphModel.CreateEdge(stack1.InputPorts[0], stack0.OutputPorts[0]);

            var b = new RoslynTranslator(Stencil);
            Assert.Throws<LoopDetectedException>(() => b.Translate(GraphModel, CompilationOptions.Default));
        }

        Mock<IVariableDeclarationModel> CreateLocalVariableDeclarationMock()
        {
            var mock = new Mock<IVariableDeclarationModel>();
            mock.Setup(decl => decl.DataType).Returns(typeof(int).GenerateTypeHandle(GraphModel.Stencil));
            mock.Setup(decl => decl.VariableType).Returns(VariableType.FunctionVariable);
            mock.Setup(decl => decl.Name).Returns("varA");
            mock.Setup(decl => decl.VariableName).Returns("varA");
            //mock.Setup(decl => decl.requiresInitialization).Returns(false);
            mock.Setup(decl => decl.InitializationModel).Returns((IConstantNodeModel)null);
            return mock;
        }

        [Test]
        public void Test_Variable_Name_Declaration()
        {
            //ARRANGE
            var translator = new RoslynTranslator(Stencil);

            Mock<IVariableDeclarationModel> mock = CreateLocalVariableDeclarationMock();

            //ACT
            var declaration = mock.Object.DeclareLocalVariable(translator);

            //ASSERT
            var declaredVariableName = declaration
                .DescendantNodes().OfType<VariableDeclarationSyntax>()
                .First()
                .Variables.First().Identifier.Value;

            Assert.That(declaredVariableName, Is.EqualTo("varA"));
        }

        [Test]
        public void Test_Variable_Type_Declaration()
        {
            //ARRANGE
            var translator = new RoslynTranslator(Stencil);

            Mock<IVariableDeclarationModel> mock = CreateLocalVariableDeclarationMock();

            //ACT
            var declaration = mock.Object.DeclareLocalVariable(translator);

            //ASSERT
            var declaredVariableTypename = declaration
                .DescendantNodes().OfType<VariableDeclarationSyntax>()
                .First()
                .Type.ToFullString();

            Assert.That(declaredVariableTypename, Is.EqualTo("int"));
        }

        [Test]
        public void Test_Initialized_Local_Variable()
        {
            //ARRANGE
            var translator = new RoslynTranslator(Stencil);

            Mock<IVariableDeclarationModel> mock = CreateLocalVariableDeclarationMock();
            //mock.Setup(decl => decl.requiresInitialization).Returns(true);
            mock.Setup(decl => decl.InitializationModel).Returns(GraphModel.CreateConstantNode("var1_init", typeof(int).GenerateTypeHandle(GraphModel.Stencil),
                Vector2.zero, SpawnFlags.Orphan | SpawnFlags.Default));

            //ACT
            var variableDeclarationSyntax = mock.Object.DeclareLocalVariable(translator);

            //ASSERT
            var numberOfInitStatement = variableDeclarationSyntax
                .DescendantNodes().OfType<EqualsValueClauseSyntax>()
                .Count();

            Assert.That(numberOfInitStatement, Is.EqualTo(1));
        }

        [Test]
        public void Test_Initialized_Local_Variable_Is_Implicitly_Typed()
        {
            //ARRANGE
            var translator = new RoslynTranslator(Stencil);

            Mock<IVariableDeclarationModel> mock = CreateLocalVariableDeclarationMock();
            //mock.Setup(decl => decl.requiresInitialization).Returns(true);
            mock.Setup(decl => decl.InitializationModel).Returns(GraphModel.CreateConstantNode("var1_init", typeof(int).GenerateTypeHandle(GraphModel.Stencil),
                Vector2.zero, SpawnFlags.Orphan | SpawnFlags.Default));

            //ACT
            var variableDeclarationSyntax = mock.Object.DeclareLocalVariable(translator);

            //ASSERT
            var isImplicitlyType = variableDeclarationSyntax
                .DescendantNodes().OfType<VariableDeclarationSyntax>()
                .First().Type.IsVar;

            Assert.That(isImplicitlyType, Is.True);
        }

        Mock<IVariableDeclarationModel> CreateFieldDeclarationMock()
        {
            var mock = new Mock<IVariableDeclarationModel>();
            mock.Setup(decl => decl.DataType).Returns(typeof(int).GenerateTypeHandle(GraphModel.Stencil));
            mock.Setup(decl => decl.VariableType).Returns(VariableType.FunctionVariable);
            mock.Setup(decl => decl.Name).Returns("fieldA");
            mock.Setup(decl => decl.VariableName).Returns("fieldA");
            //mock.Setup(decl => decl.requiresInitialization).Returns(false);
            mock.Setup(decl => decl.InitializationModel).Returns((IConstantNodeModel)null);
            return mock;
        }

        [Test]
        public void Test_Field_Name_Declaration()
        {
            //ARRANGE
            var translator = new RoslynTranslator(GraphModel.Stencil);

            Mock<IVariableDeclarationModel> mock = CreateFieldDeclarationMock();

            //ACT
            var declaration = mock.Object.DeclareField(translator);

            //ASSERT
            var declaredFieldName = declaration
                .DescendantNodes().OfType<VariableDeclarationSyntax>()
                .First()
                .Variables.First().Identifier.Value;

            Assert.That(declaredFieldName, Is.EqualTo("fieldA"));
        }

        [Test]
        public void Test_Field_Type_Declaration()
        {
            //ARRANGE
            var translator = new RoslynTranslator(Stencil);

            Mock<IVariableDeclarationModel> mock = CreateFieldDeclarationMock();

            //ACT
            var declaration = mock.Object.DeclareField(translator);

            //ASSERT
            var declaredFieldTypename = declaration
                .DescendantNodes().OfType<VariableDeclarationSyntax>()
                .First()
                .Type.ToFullString();

            Assert.That(declaredFieldTypename, Is.EqualTo("int"));
        }

        [Test]
        public void Test_Public_Field_Declaration()
        {
            //ARRANGE
            var translator = new RoslynTranslator(Stencil);

            Mock<IVariableDeclarationModel> mock = CreateFieldDeclarationMock();
            mock.Setup(decl => decl.IsExposed).Returns(true);

            //ACT
            var declaration = mock.Object.DeclareField(translator);

            //ASSERT
            var fieldScope = declaration
                .Modifiers.First().ValueText;

            Assert.That(fieldScope, Is.EqualTo("public"));
        }

        [Test]
        public void Test_Private_Field_Declaration()
        {
            //ARRANGE
            var translator = new RoslynTranslator(Stencil);

            Mock<IVariableDeclarationModel> mock = CreateFieldDeclarationMock();
            mock.Setup(decl => decl.IsExposed).Returns(false);

            //ACT
            var declaration = mock.Object.DeclareField(translator);

            //ASSERT
            var fieldScope = declaration
                .Modifiers.First().ValueText;

            Assert.That(fieldScope, Is.EqualTo("private"));
        }

        [Test]
        public void Test_Initialized_Field()
        {
            //ARRANGE
            var translator = new RoslynTranslator(Stencil);

            Mock<IVariableDeclarationModel> mock = CreateFieldDeclarationMock();
            //mock.Setup(decl => decl.requiresInitialization).Returns(true);
            mock.Setup(decl => decl.InitializationModel).Returns(GraphModel.CreateConstantNode("var1_init", typeof(int).GenerateTypeHandle(GraphModel.Stencil),
                Vector2.zero, SpawnFlags.Orphan | SpawnFlags.Default));

            //ACT
            var variableDeclarationSyntax = mock.Object.DeclareField(translator);

            //ASSERT
            var numberOfInitStatement = variableDeclarationSyntax
                .DescendantNodes().OfType<EqualsValueClauseSyntax>()
                .Count();

            Assert.That(numberOfInitStatement, Is.EqualTo(1));
        }

        [Test]
        public void Test_Initialized_Field_Is_Explicitly_Typed()
        {
            //ARRANGE
            var translator = new RoslynTranslator(Stencil);

            Mock<IVariableDeclarationModel> mock = CreateFieldDeclarationMock();
            //mock.Setup(decl => decl.requiresInitialization).Returns(true);
            mock.Setup(decl => decl.InitializationModel).Returns(GraphModel.CreateConstantNode("var1_init", typeof(int).GenerateTypeHandle(GraphModel.Stencil),
                Vector2.zero, SpawnFlags.Orphan | SpawnFlags.Default));

            //ACT
            var fieldDeclarationSyntax = mock.Object.DeclareField(translator);

            //ASSERT
            var isImplicitlyType = fieldDeclarationSyntax
                .DescendantNodes().OfType<VariableDeclarationSyntax>()
                .First().Type.IsVar;

            Assert.That(isImplicitlyType, Is.False);
        }
    }
}
