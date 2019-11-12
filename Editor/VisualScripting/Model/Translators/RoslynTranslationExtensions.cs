using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;

namespace UnityEditor.VisualScripting.Model.Translators
{
    [GraphtoolsExtensionMethods]
    public static class RoslynTranslatorExtensions
    {
        public static IEnumerable<SyntaxNode> BuildThisNode(this RoslynTranslator translator, ThisNodeModel model, IPortModel portModel)
        {
            yield return SyntaxFactory.ThisExpression();
        }

        public static IEnumerable<SyntaxNode> BuildGetPropertyNode(this RoslynTranslator translator, GetPropertyGroupNodeModel model, IPortModel portModel)
        {
            var instancePort = model.InstancePort;
            var input = !instancePort.Connected ? SyntaxFactory.ThisExpression() : translator.BuildPort(instancePort).SingleOrDefault();

            if (input == null)
                yield break;

            var member = model.Members.FirstOrDefault(m => m.GetId() == portModel.UniqueId);
            if (member.Path == null || member.Path.Count == 0)
                yield break;

            var access = RoslynBuilder.MemberReference(input, member.Path[0]);
            for (int i = 1; i < member.Path.Count; i++)
            {
                access = RoslynBuilder.MemberReference(access, member.Path[i]);
            }

            yield return access;
        }

        public static IEnumerable<SyntaxNode> BuildBinaryOperator(this RoslynTranslator translator, BinaryOperatorNodeModel model, IPortModel portModel)
        {
            yield return RoslynBuilder.BinaryOperator(model.kind,
                translator.BuildPort(model.InputPortA).SingleOrDefault(),
                translator.BuildPort(model.InputPortB).SingleOrDefault());
        }

        public static IEnumerable<SyntaxNode> BuildUnaryOperator(this RoslynTranslator translator, UnaryOperatorNodeModel model, IPortModel portModel)
        {
            yield return RoslynBuilder.UnaryOperator(model.kind, translator.BuildPort(model.InputPort).SingleOrDefault());
        }

        public static IEnumerable<SyntaxNode> BuildReturn(this RoslynTranslator translator, ReturnNodeModel returnModel, IPortModel portModel)
        {
            if (returnModel.InputPort == null)
                yield return SyntaxFactory.ReturnStatement();
            else
                yield return SyntaxFactory.ReturnStatement(
                    translator.BuildPort(returnModel.InputPort).FirstOrDefault() as ExpressionSyntax);
        }

        public static IEnumerable<SyntaxNode> BuildSetVariable(this RoslynTranslator translator, SetVariableNodeModel statement, IPortModel portModel)
        {
            var decl = translator.BuildPort(statement.InstancePort).SingleOrDefault();
            var value = translator.BuildPort(statement.ValuePort).SingleOrDefault();
            yield return decl == null || value == null ? null : RoslynBuilder.Assignment(decl, value);
        }

        public static IEnumerable<SyntaxNode> BuildInlineExpression(this RoslynTranslator translator, InlineExpressionNodeModel v, IPortModel portModel)
        {
            var expressionCode = "var ___exp = (" + v.Expression + ")";
            var syntaxTree = CSharpSyntaxTree.ParseText(expressionCode);
            var buildInlineExpression = syntaxTree.GetRoot().DescendantNodesAndSelf().OfType<ParenthesizedExpressionSyntax>().FirstOrDefault();
            yield return buildInlineExpression;
        }

        public static IEnumerable<SyntaxNode> BuildVariable(this RoslynTranslator translator, IVariableModel v, IPortModel portModel)
        {
            if (v is IConstantNodeModel constantNodeModel)
            {
                if (constantNodeModel.ObjectValue != null)
                {
                    if (constantNodeModel is IStringWrapperConstantModel)
                        yield return translator.Constant(constantNodeModel.ObjectValue.ToString(), translator.Stencil);
                    else
                        yield return translator.Constant(constantNodeModel.ObjectValue, translator.Stencil);
                }

                yield break;
            }

            if (translator.InMacro.Count > 0 && v.DeclarationModel.VariableType == VariableType.GraphVariable && v.DeclarationModel.Modifiers == ModifierFlags.ReadOnly)
            {
                MacroRefNodeModel oldValue = translator.InMacro.Pop();

                var syntaxNodes = translator.BuildPort(oldValue.InputsById[v.DeclarationModel.VariableName]);
                translator.InMacro.Push(oldValue);
                foreach (var syntaxNode in syntaxNodes)
                    yield return syntaxNode;
                yield break;
            }

            switch (v.DeclarationModel.VariableType)
            {
                case VariableType.FunctionVariable:
                case VariableType.GraphVariable:
                case VariableType.ComponentQueryField:
                    yield return RoslynBuilder.LocalVariableReference(v.DeclarationModel.Name);
                    break;

                case VariableType.FunctionParameter:
                    yield return RoslynBuilder.ArgumentReference(v.DeclarationModel.Name);
                    break;

//                case VariableType.Literal:
//                case VariableType.InlineExpression:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static IEnumerable<SyntaxNode> BuildLoop(this RoslynTranslator translator, LoopNodeModel statement, IPortModel portModel)
        {
            IPortModel outputPortModel = statement.OutputPort;
            if (outputPortModel == null)
                yield break;

            LoopStackModel loopStackModel =
                outputPortModel.ConnectionPortModels.FirstOrDefault()?.NodeModel as LoopStackModel;

            if (loopStackModel == null)
                yield break;

            foreach (var statementSyntax in translator.BuildNode(loopStackModel))
                yield return statementSyntax;
        }

        public static IEnumerable<SyntaxNode> BuildWhile(this RoslynTranslator translator, WhileHeaderModel whileHeaderModelStatement, IPortModel portModel)
        {
            if (whileHeaderModelStatement.IndexVariableDeclarationModel != null)
            {
                yield return whileHeaderModelStatement.IndexVariableDeclarationModel.DeclareLoopIndexVariable();
            }

            var whileBlock = SyntaxFactory.Block();

            foreach (var localDeclaration in BuildLocalDeclarations(translator, whileHeaderModelStatement))
                whileBlock = whileBlock.AddStatements(localDeclaration);

            translator.BuildStack(whileHeaderModelStatement, ref whileBlock, StackExitStrategy.Continue);

            IPortModel loopExecutionInputPortModel = whileHeaderModelStatement.InputPort;
            IPortModel insertLoopPortModel = loopExecutionInputPortModel?.ConnectionPortModels?.FirstOrDefault();
            var insertLoopNodeModel = insertLoopPortModel?.NodeModel as IHasMainInputPort;
            IPortModel conditionInputPortModel = insertLoopNodeModel?.InputPort;

            SeparatedSyntaxList<ExpressionSyntax> incrementExpressions = SyntaxFactory.SeparatedList<ExpressionSyntax>();
            if (whileHeaderModelStatement.IndexVariableDeclarationModel != null)
            {
                incrementExpressions =
                    SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                        SyntaxFactory.PostfixUnaryExpression(
                            SyntaxKind.PostIncrementExpression,
                            SyntaxFactory.IdentifierName(whileHeaderModelStatement.IndexVariableDeclarationModel.Name)));
            }

            yield return SyntaxFactory.ForStatement(null, SyntaxFactory.SeparatedList<ExpressionSyntax>(),
                translator.BuildPort(conditionInputPortModel).SingleOrDefault() as ExpressionSyntax,
                incrementExpressions,
                whileBlock);
        }

        public static IEnumerable<SyntaxNode> BuildForEach(this RoslynTranslator translator, ForEachHeaderModel forEachHeaderModelStatement,
            IPortModel portModel)
        {
            IPortModel loopExecutionInputPortModel = forEachHeaderModelStatement.InputPort;
            IPortModel insertLoopPortModel = loopExecutionInputPortModel?.ConnectionPortModels?.FirstOrDefault();
            var insertLoopNodeModel = insertLoopPortModel?.NodeModel as IHasMainInputPort;
            IPortModel collectionInputPortModel = insertLoopNodeModel?.InputPort;

            if (collectionInputPortModel == null || !collectionInputPortModel.Connected)
                yield break;

            var collectionName = translator.MakeUniqueName("Collection");
            SyntaxNode collectionSyntax =
                translator.BuildPort(collectionInputPortModel).SingleOrDefault();
            yield return RoslynBuilder.DeclareLoopCollectionVariable(collectionSyntax, collectionName);

            if (forEachHeaderModelStatement.IndexVariableDeclarationModel != null)
            {
                yield return forEachHeaderModelStatement.IndexVariableDeclarationModel.DeclareLoopIndexVariable(-1);
            }

            if (forEachHeaderModelStatement.CountVariableDeclarationModel != null)
            {
                var collectionInput =
                    translator.BuildPort(collectionInputPortModel).SingleOrDefault() as ExpressionSyntax;
                yield return forEachHeaderModelStatement.CountVariableDeclarationModel.DeclareLoopCountVariable(
                    collectionInput,
                    collectionName,
                    translator);
            }

            var forEachBlock = SyntaxFactory.Block();

            foreach (var localDeclaration in BuildLocalDeclarations(translator, forEachHeaderModelStatement))
                forEachBlock = forEachBlock.AddStatements(localDeclaration);


            if (forEachHeaderModelStatement.IndexVariableDeclarationModel != null)
            {
                forEachBlock = forEachBlock.AddStatements(SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.PostfixUnaryExpression(
                        SyntaxKind.PostIncrementExpression,
                        SyntaxFactory.IdentifierName(forEachHeaderModelStatement.IndexVariableDeclarationModel.Name)))
                );
            }

            translator.BuildStack(forEachHeaderModelStatement, ref forEachBlock, StackExitStrategy.Continue);

            var itemModel = forEachHeaderModelStatement.ItemVariableDeclarationModel;
            if (itemModel == null)
                yield break;

            yield return SyntaxFactory.ForEachStatement(
                SyntaxFactory.IdentifierName("var"),
                SyntaxFactory.Identifier(itemModel.VariableName),
                SyntaxFactory.IdentifierName(collectionName),
                forEachBlock);
        }

        public static IEnumerable<SyntaxNode> BuildIfCondition(this RoslynTranslator translator, IfConditionNodeModel statement, IPortModel portModel)
        {
            // this enables more elegant code generation with no duplication
            // if() { then(); } else { else(); }
            // codeAfterIf();
            // instead of duplicating the code after the if in each branch
            // find first stack reachable from both then/else stacks
            var firstThenStack = RoslynTranslator.GetConnectedStack(statement, 0);
            var firstElseStack = RoslynTranslator.GetConnectedStack(statement, 1);
            var endStack = RoslynTranslator.FindCommonDescendant(statement.ParentStackModel, firstThenStack, firstElseStack);
            if (endStack != null)
            {
                // building the branches will stop at the common descendant
                translator.EndStack = endStack;
                // Debug.Log($"If in stack {statement.parentStackModel} Common descendant: {endStack}");
            }

            // ie. follow outputs, find all stacks with multiple inputs, compare them until finding the common one if it exists
            // BuildStack checks the m_EndStack field, returning when recursing on it
            // the parent buildStack call will then continue on this end stack


            var origBuiltStacks = translator.BuiltStacks;
            translator.BuiltStacks = new HashSet<IStackModel>(origBuiltStacks);
            HashSet<IStackModel> partialStacks = new HashSet<IStackModel>();

            StatementSyntax syntax = null;
            // construct multiple if else if else ... from right to left, starting with the last else
            foreach (var conditionWithAction in statement.ConditionsWithActionsPorts.Reverse())
            {
                var stack = RoslynTranslator.GetConnectedStack(conditionWithAction.Action);
                BlockSyntax block = SyntaxFactory.Block();
                if (endStack == null || endStack != stack) // stop before the end stack
                    translator.BuildStack(stack, ref block, StackExitStrategy.Inherit);
                if (conditionWithAction.Condition == null) // last else
                {
                    syntax = block;
                }
                else // if = if() { current statement } else { prev statement that might be an if }
                {
                    var condition = (ExpressionSyntax)translator.BuildPort(conditionWithAction.Condition).SingleOrDefault();
                    syntax = SyntaxFactory.IfStatement(condition, block)
                        .WithElse(SyntaxFactory.ElseClause(syntax));
                }

                partialStacks.UnionWith(translator.BuiltStacks);
                translator.BuiltStacks = new HashSet<IStackModel>(origBuiltStacks);
            }

            translator.BuiltStacks = partialStacks;

            yield return syntax;
        }

        static InvocationExpressionSyntax FunctionInvokeExpression(string genericTypeName, string methodName, ExpressionSyntax instance, List<ArgumentSyntax> argumentList)
        {
            var invocationExpressionSyntax = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    instance,
                    SyntaxFactory.IdentifierName("Call")));

            var syntaxNodeList = new SyntaxNodeOrToken[]
            {
                SyntaxFactory.Argument(instance),
                SyntaxFactory.Token(SyntaxKind.CommaToken),
                SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal(methodName)))
            };

            invocationExpressionSyntax = invocationExpressionSyntax.WithArgumentList(
                SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList<ArgumentSyntax>(syntaxNodeList)));

            return invocationExpressionSyntax;
        }

        public static IEnumerable<SyntaxNode> BuildFunctionRefCall(this RoslynTranslator translator, FunctionRefCallNodeModel call, IPortModel portModel)
        {
            if (call.Function.Destroyed)
                yield break;
            ExpressionSyntax instance = BuildArgumentList(translator, call, out var argumentList);
            if (!call.Function.IsInstanceMethod)
                instance = SyntaxFactory.IdentifierName(((VSGraphModel)call.Function.GraphModel).TypeName);
            InvocationExpressionSyntax invocationExpressionSyntax = null;
            // not a VisualBehaviour reference method call
            if (invocationExpressionSyntax == null)
            {
                invocationExpressionSyntax = instance == null ||
                    (instance is LiteralExpressionSyntax &&
                        (instance).IsKind(SyntaxKind.NullLiteralExpression))
                    ? SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(call.Function.CodeTitle))
                    : SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        instance,
                        SyntaxFactory.IdentifierName(call.Function.CodeTitle)))
                ;

                invocationExpressionSyntax = invocationExpressionSyntax.WithArgumentList(
                    SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(argumentList)));
            }

            if (portModel == null)
                yield return SyntaxFactory.ExpressionStatement(
                    invocationExpressionSyntax)
                    .NormalizeWhitespace();
            else
                yield return invocationExpressionSyntax.NormalizeWhitespace();
        }

        public static IEnumerable<SyntaxNode> BuildFunctionCall(this RoslynTranslator translator, FunctionCallNodeModel call, IPortModel portModel)
        {
            if (call.MethodInfo == null)
                yield break;

            var instance = BuildArgumentList(translator, call, out var argumentList);

            var typeArgumentList = new List<TypeSyntax>();
            if (call.MethodInfo.IsGenericMethod)
            {
                foreach (var typeArgument in call.TypeArguments)
                {
                    typeArgumentList.Add(TypeSystem.BuildTypeSyntax(typeArgument.Resolve(translator.Stencil)));
                }
            }

            TypeArgumentListSyntax typeArgList = null;
            if (typeArgumentList.Any())
                typeArgList = SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList(typeArgumentList.First()));

            SyntaxNode method = RoslynBuilder.MethodInvocation(call.Title, call.MethodInfo, instance, argumentList, typeArgList);

            yield return method;
        }

        static ExpressionSyntax BuildArgumentList(RoslynTranslator translator, IFunctionCallModel call, out List<ArgumentSyntax> argumentList)
        {
            ExpressionSyntax instance = null;
            if (call is IHasInstancePort modelWithInstancePort && modelWithInstancePort.InstancePort != null)
            {
                instance = (ExpressionSyntax)translator.BuildPort(modelWithInstancePort.InstancePort).SingleOrDefault();
            }

            argumentList = new List<ArgumentSyntax>();
            foreach (IPortModel port in call.GetParameterPorts())
            {
                var syntaxNode = translator.BuildPort(port).SingleOrDefault();
                if (syntaxNode != null)
                {
                    var argumentNode = syntaxNode as ArgumentSyntax ?? SyntaxFactory.Argument(syntaxNode as ExpressionSyntax);
                    argumentList.Add(argumentNode);
                }
            }

            return instance;
        }

        public static IEnumerable<SyntaxNode> BuildSetPropertyNode(this RoslynTranslator translator, SetPropertyGroupNodeModel model, IPortModel portModel)
        {
            SyntaxNode leftHand;

            IPortModel instancePort = model.InstancePort;
            if (!instancePort.Connected)
                leftHand = SyntaxFactory.ThisExpression();
            else
                leftHand = translator.BuildPort(instancePort).SingleOrDefault();

            foreach (var member in model.Members)
            {
                string memberId = member.GetId();
                IPortModel inputPort = model.InputsById[memberId];

                SyntaxNode rightHandExpression = translator.BuildPort(inputPort).SingleOrDefault();
                if (rightHandExpression == null)
                    continue;

                MemberAccessExpressionSyntax access = RoslynBuilder.MemberReference(leftHand, member.Path[0]);
                for (int i = 1; i < member.Path.Count; i++)
                {
                    access = RoslynBuilder.MemberReference(access, member.Path[i]);
                }

                yield return SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, access, rightHandExpression as ExpressionSyntax);
            }
        }

        public static IEnumerable<SyntaxNode> BuildMethod(this RoslynTranslator roslynTranslator, KeyDownEventModel stack, IPortModel portModel)
        {
            BlockSyntax block = SyntaxFactory.Block();
            roslynTranslator.BuildStack(stack, ref block);

            string methodName;
            switch (stack.mode)
            {
                case KeyDownEventModel.EventMode.Pressed:
                    methodName = nameof(Input.GetKeyDown);
                    break;
                case KeyDownEventModel.EventMode.Released:
                    methodName = nameof(Input.GetKeyUp);
                    break;
                default:
                    methodName = nameof(Input.GetKey);
                    break;
            }

            var conditionExpression = (ExpressionSyntax)roslynTranslator.BuildPort(stack.KeyPort).Single();

            IfStatementSyntax keydownCheck = (SyntaxFactory.IfStatement(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(nameof(Input)),
                        SyntaxFactory.IdentifierName(methodName)))
                    .WithArgumentList(
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Argument(
                                    conditionExpression)))),
                block)
                .NormalizeWhitespace());
            roslynTranslator.AddEventRegistration(keydownCheck);
            yield break;
        }

        public static IEnumerable<SyntaxNode> BuildMethod(this RoslynTranslator roslynTranslator, IFunctionModel stack, IPortModel portModel)
        {
            roslynTranslator.ClearBuiltStacks();
            var generatedName = roslynTranslator.MakeUniqueName(stack.CodeTitle);
            var methodSyntaxNode = RoslynBuilder.DeclareMethod(
                generatedName, AccessibilityFlags.Public, stack.ReturnType.Resolve(roslynTranslator.Stencil));
            var localDeclarationNodes = BuildLocalDeclarations(roslynTranslator, stack);
            var argumentNodes = BuildArguments(roslynTranslator.Stencil, stack);

            methodSyntaxNode = methodSyntaxNode.WithParameterList(SyntaxFactory.ParameterList(
                SyntaxFactory.SeparatedList(argumentNodes.ToArray())));
            methodSyntaxNode = methodSyntaxNode.WithBody(SyntaxFactory.Block(localDeclarationNodes.ToArray()));

            if (stack.EnableProfiling)
            {
                throw new NotImplementedException("BuildMethod Profiling not implemented");
//                methodSyntaxNode = methodSyntaxNode.WithAdditionalAnnotations(InstrumentForProfiling.profilingAnnotation);
            }

            BlockSyntax stackBlock = SyntaxFactory.Block();
            roslynTranslator.BuildStack(stack, ref stackBlock);
            foreach (var statement in stackBlock.Statements)
            {
                methodSyntaxNode = methodSyntaxNode.AddBodyStatements(statement);
            }

            yield return methodSyntaxNode;
        }

        public static List<ParameterSyntax> BuildArguments(Stencil stencil, IFunctionModel stack)
        {
            var argumentNodes = new List<ParameterSyntax>();
            foreach (var paramDecl in stack.FunctionParameterModels)
            {
                switch (paramDecl.VariableType)
                {
                    case VariableType.FunctionParameter:
                        argumentNodes.Add(SyntaxFactory.Parameter(
                            SyntaxFactory.Identifier(paramDecl.Name))
                            .WithType(
                                TypeSystem.BuildTypeSyntax(paramDecl.DataType.Resolve(stencil))));
                        break;
                }
            }

            return argumentNodes;
        }

        public static List<StatementSyntax> BuildLocalDeclarations(RoslynTranslator roslynTranslator, IFunctionModel stack)
        {
            var localDeclarationNodes = new List<StatementSyntax>();
            if (stack.VariableDeclarations == null)
                return localDeclarationNodes;

            localDeclarationNodes.AddRange(stack.VariableDeclarations
                .Where(localDecl => localDecl.VariableType == VariableType.FunctionVariable)
                .Select(localDecl => localDecl.DeclareLocalVariable(roslynTranslator)));

            return localDeclarationNodes;
        }

        public static IEnumerable<SyntaxNode> BuildStaticConstantNode(this RoslynTranslator translator, SystemConstantNodeModel model, IPortModel portModel)
        {
            yield return SyntaxFactory.QualifiedName(
                SyntaxFactory.IdentifierName(model.DeclaringType.Name(translator.Stencil)),
                SyntaxFactory.IdentifierName(model.Identifier));
        }

        public static IEnumerable<SyntaxNode> BuildMacroRefNode(this RoslynTranslator translator,
            MacroRefNodeModel model, IPortModel portModel)
        {
            if (model.Macro == null)
            {
                translator.AddError(
                    model,
                    $"The asset of macro node {model.Title} is missing.");
                return Enumerable.Empty<SyntaxNode>();
            }

            var variableDeclarations = portModel.Direction == Direction.Input
                ? model.DefinedInputVariables
                : model.DefinedOutputVariables;
            var declaration = variableDeclarations.Single(v => v.VariableName == portModel.UniqueId);
            translator.InMacro.Push(model);
            var variableNodeModel = ((VSGraphModel)declaration.GraphModel)
                .FindUsages((VariableDeclarationModel)declaration)
                .Single();
            var returnValue = translator.BuildPort(variableNodeModel.OutputPort);
            translator.InMacro.Pop();
            return returnValue;
        }
    }
}
