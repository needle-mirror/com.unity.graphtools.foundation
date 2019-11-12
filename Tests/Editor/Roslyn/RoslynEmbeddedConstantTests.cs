using System;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEditor.VisualScripting.Model.Translators;
using UnityEngine;
using CompilationOptions = UnityEngine.VisualScripting.CompilationOptions;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.VisualScriptingTests.Roslyn
{
    class RoslynEmbeddedConstantTests : BaseFixture
    {
        protected override bool CreateGraphOnStartup => true;

        [Test]
        public void Test_EmbeddedConstantIsUsedWhenDisconnected([Values] TestingMode mode)
        {
            const float outerValue = 42f;
            const float innerValue = 347f;

            // make sure that we really test values that are not default
            Assume.That(outerValue, Is.Not.EqualTo(default(float)));
            Assume.That(innerValue, Is.Not.EqualTo(default(float)));

            var stencil = GraphModel.Stencil;

            FunctionModel a = GraphModel.CreateFunction("A", Vector2.zero);

            // Debug.Log(...)
            MethodInfo logMethod = typeof(Debug).GetMethod(nameof(Debug.Log), new[] { typeof(object) });
            Assume.That(logMethod, Is.Not.Null);
            FunctionCallNodeModel log = a.CreateStackedNode<FunctionCallNodeModel>("Log", 0, SpawnFlags.Default, n => n.MethodInfo = logMethod);
            var logParameterPort = log.GetParameterPorts().Single();

            // Math.Abs(...)
            MethodInfo absMethod = typeof(Mathf).GetMethod(nameof(Mathf.Abs), new[] { typeof(float) });
            Assume.That(absMethod, Is.Not.Null);
            FunctionCallNodeModel abs = GraphModel.CreateNode<FunctionCallNodeModel>("Abs", Vector2.zero, SpawnFlags.Default, n => n.MethodInfo = absMethod);
            var absParameterPort = abs.GetParameterPorts().Single();
            ((FloatConstantModel)abs.InputConstantsById[absParameterPort.UniqueId]).value = innerValue;

            GraphModel.CreateEdge(logParameterPort, abs.OutputPort);

            // float
            IConstantNodeModel outerFloat = GraphModel.CreateConstantNode("float42", typeof(float).GenerateTypeHandle(stencil), Vector2.zero);
            Assume.That(outerFloat, Is.Not.Null);
            ((FloatConstantModel)outerFloat).value = outerValue;

            string innerFloatString = SyntaxFactory.LiteralExpression(
                SyntaxKind.NumericLiteralExpression,
                SyntaxFactory.Literal(innerValue)).ToFullString();
            string outerFloatString = SyntaxFactory.LiteralExpression(
                SyntaxKind.NumericLiteralExpression,
                SyntaxFactory.Literal(outerValue)).ToFullString();

            TestPrereqActionPostreq(mode,
                () =>
                {
                    // outer float disconnected, check that we use the inner value
                    SyntaxNode astRoot = CompileCurrentGraphModel();
                    LiteralExpressionSyntax literalInsideLogAbs = GetLiteralInsideLogAbs(astRoot);
                    Assert.That(literalInsideLogAbs.ToFullString(), Is.EqualTo(innerFloatString));
                    return new CreateEdgeAction(absParameterPort, outerFloat.OutputPort);
                },
                () =>
                {
                    // outer float connected, check that we use the outer value
                    SyntaxNode astRoot = CompileCurrentGraphModel();
                    LiteralExpressionSyntax literalInsideLogAbs = GetLiteralInsideLogAbs(astRoot);
                    Assert.That(literalInsideLogAbs.ToFullString(), Is.EqualTo(outerFloatString));
                });
        }

        SyntaxNode CompileCurrentGraphModel()
        {
            var roslynTr = new RoslynTranslator(GraphModel.Stencil);
            var ast = roslynTr.Translate(GraphModel, CompilationOptions.Profiling);
            return ast.GetRoot();
        }

        static LiteralExpressionSyntax GetLiteralInsideLogAbs(SyntaxNode astRoot)
        {
            MethodDeclarationSyntax method = astRoot.DescendantNodes().OfType<MethodDeclarationSyntax>().First(n => n.Identifier.ValueText == "A");
            Assert.NotNull(method);
            var statement = method.Body.Statements.First() as ExpressionStatementSyntax;
            Assert.NotNull(statement);
            var debugExpr = statement.Expression as InvocationExpressionSyntax;
            Assert.NotNull(debugExpr);
            var absExpr = debugExpr.ArgumentList.Arguments.Single().Expression as InvocationExpressionSyntax;
            Assert.NotNull(absExpr);
            LiteralExpressionSyntax literal = absExpr.ArgumentList.Arguments.Single().Expression as LiteralExpressionSyntax;
            Assert.NotNull(literal);
            return literal;
        }
    }
}
