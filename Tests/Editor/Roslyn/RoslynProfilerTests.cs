using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEditor.VisualScripting.Model.Translators;
using UnityEngine;
using CompilationOptions = UnityEngine.VisualScripting.CompilationOptions;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.VisualScriptingTests.Roslyn
{
    class RoslynProfilerTests : BaseFixture
    {
        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(ClassStencil);

        [Test, Ignore("mb")]
        public void Test_Profile()
        {
            // turn on profiling
            FunctionModel isIntEvenFunction = CreateIsIntEvenFunction();

            // enable profiling for this function
            isIntEvenFunction.EnableProfiling = true;

            // needed to set the owning function of each stack
            new PortInitializationTraversal().VisitGraph(GraphModel);

            // compile graph
            var roslynTr = new RoslynTranslator(Stencil);
            var ast = roslynTr.Translate(GraphModel, CompilationOptions.Profiling);
            SyntaxNode astRoot = ast.GetRoot();

            // check there's only one IsIntEven method
            IEnumerable<MethodDeclarationSyntax> methods = astRoot.DescendantNodes().OfType<MethodDeclarationSyntax>().ToList();
            Assert.That(methods.Count, Is.EqualTo(1));
            MethodDeclarationSyntax method = methods.First();

            // check there's only a CustomSampler declaration and then a try/finally statement including everything
            BlockSyntax body = method.Body;
            Assert.That(body.Statements.Count, Is.EqualTo(2));
            StatementSyntax statement1 = body.Statements[0];
            Assert.That(statement1, Is.TypeOf(typeof(LocalDeclarationStatementSyntax)));

            StatementSyntax statement2 = body.Statements[1];
            Assert.That(statement2, Is.TypeOf(typeof(TryStatementSyntax)));

            TryStatementSyntax tryStatement = (TryStatementSyntax)statement2;

            // check that there is code inside the try and finally statements
            Assert.That(tryStatement.Block.Statements.Count, Is.GreaterThan(0));
            Assert.That(tryStatement.Finally.Block.Statements.Count, Is.GreaterThan(0));
        }

        // create a function bool IsIntEven(int i)
        //      if ((i % 2) == 0) { return true; } else { return false; }
        // the goal is to have 2 different return nodes depending on a parameter
        FunctionModel CreateIsIntEvenFunction()
        {
            // define function
            FunctionModel method = GraphModel.CreateFunction("IsIntEven", Vector2.zero);
            method.ReturnType = typeof(bool).GenerateTypeHandle(GraphModel.Stencil);
            VariableDeclarationModel paramI = method.CreateAndRegisterFunctionParameterDeclaration("i", typeof(int).GenerateTypeHandle(GraphModel.Stencil));

            // add if/then/else structure
            IfConditionNodeModel if0 = CreateIfThenElseStacks(method, out var then0, out var else0);

            // if (i % 2 == 0)
            var binOpNode = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Modulo, Vector2.zero);
            IVariableModel varI = GraphModel.CreateVariableNode(paramI, Vector2.left);
            var const2 = CreateConstantIntNode(2);
            GraphModel.CreateEdge(binOpNode.InputPortA, varI.OutputPort);
            GraphModel.CreateEdge(binOpNode.InputPortB, const2.OutputPort);
            var equalNode = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Equals, Vector2.zero);
            var const0 = CreateConstantIntNode(0);
            GraphModel.CreateEdge(equalNode.InputPortA, binOpNode.OutputPort);
            GraphModel.CreateEdge(equalNode.InputPortB, const0.OutputPort);
            GraphModel.CreateEdge(if0.IfPort, equalNode.OutputPort);

            // then return true
            var returnTrue = then0.CreateStackedNode<ReturnNodeModel>("return true");
            var constTrue = CreateConstantBoolNode(true);
            GraphModel.CreateEdge(returnTrue.InputPort, constTrue.OutputPort);

            // else return false
            var returnFalse = else0.CreateStackedNode<ReturnNodeModel>("return false");
            var constFalse = CreateConstantBoolNode(false);
            GraphModel.CreateEdge(returnFalse.InputPort, constFalse.OutputPort);
            return method;
        }

        IfConditionNodeModel CreateIfThenElseStacks(StackBaseModel ifStack, out StackBaseModel thenStack, out StackBaseModel elseStack)
        {
            var ifNode = ifStack.CreateStackedNode<IfConditionNodeModel>("if");

            thenStack = GraphModel.CreateStack("then", Vector2.left);
            GraphModel.CreateEdge(thenStack.InputPorts[0], ifNode.ThenPort);

            elseStack = GraphModel.CreateStack("else", Vector2.right);
            GraphModel.CreateEdge(elseStack.InputPorts[0], ifNode.ElsePort);
            return ifNode;
        }

        IConstantNodeModel CreateConstantIntNode(int value)
        {
            IConstantNodeModel constNode = GraphModel.CreateConstantNode("int", typeof(int).GenerateTypeHandle(GraphModel.Stencil), Vector2.zero);
            ((IntConstantModel)constNode).value = value;
            return constNode;
        }

        IConstantNodeModel CreateConstantBoolNode(bool value)
        {
            IConstantNodeModel constNode = GraphModel.CreateConstantNode("bool", typeof(bool).GenerateTypeHandle(GraphModel.Stencil), Vector2.zero);
            ((BooleanConstantNodeModel)constNode).value = value;
            return constNode;
        }
    }
}
