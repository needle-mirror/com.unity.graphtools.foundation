using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEngine;
using VisualScripting.Model.Common.Extensions;

namespace UnityEditor.VisualScripting.Model.Translators
{
    [PublicAPI]
    public static class RoslynBuilder
    {
        public enum VariableDeclarationType
        {
            ExplicitType,
            InferredType,
        }

        public enum AssignmentKind
        {
            Set,
            Add,
            Subtract,
            Multiply,
            Divide,
            Modulo,
            And,
            ExclusiveOr,
            Or,
            LeftShift,
            RightShift
        }

        static SyntaxKind ToSyntaxKind(this AssignmentKind kind)
        {
            switch (kind)
            {
                case AssignmentKind.Set: return SyntaxKind.SimpleAssignmentExpression;
                case AssignmentKind.Add: return SyntaxKind.AddAssignmentExpression;
                case AssignmentKind.Subtract: return SyntaxKind.SubtractAssignmentExpression;
                case AssignmentKind.Multiply: return SyntaxKind.MultiplyAssignmentExpression;
                case AssignmentKind.Divide: return SyntaxKind.DivideAssignmentExpression;
                case AssignmentKind.Modulo: return SyntaxKind.ModuloAssignmentExpression;
                case AssignmentKind.And: return SyntaxKind.ModuloAssignmentExpression;
                case AssignmentKind.ExclusiveOr: return SyntaxKind.ExclusiveOrAssignmentExpression;
                case AssignmentKind.Or: return SyntaxKind.OrAssignmentExpression;
                case AssignmentKind.LeftShift: return SyntaxKind.LeftShiftAssignmentExpression;
                case AssignmentKind.RightShift: return SyntaxKind.RightShiftAssignmentExpression;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        public static ExpressionSyntax DeclareNewObject(TypeSyntax type, IEnumerable<ArgumentSyntax> args,
            IEnumerable<AssignmentExpressionSyntax> initializers)
        {
            var creator = SyntaxFactory.ObjectCreationExpression(type);

            var argsList = args.ToList();
            if (argsList.Any())
                creator = creator.WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SeparatedList(argsList)));

            var initList = initializers.ToList();
            if (initList.Any())
                creator = creator.WithInitializer(
                    SyntaxFactory.InitializerExpression(
                        SyntaxKind.ObjectInitializerExpression,
                        SyntaxFactory.SeparatedList<ExpressionSyntax>(initList)));

            return creator;
        }

        public static FieldDeclarationSyntax DeclareField(Type fieldType, string name,
            AccessibilityFlags accessibility = AccessibilityFlags.Default)
        {
            var typeSyntax = fieldType.ToTypeSyntax();

            return DeclareField(typeSyntax, name, accessibility);
        }

        public static FieldDeclarationSyntax DeclareField(TypeSyntax typeSyntax, string name, AccessibilityFlags accessibility)
        {
            VariableDeclarationSyntax varDeclaration = SyntaxFactory.VariableDeclaration(typeSyntax);
            var varDeclarator = SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(name));

            FieldDeclarationSyntax fieldDeclaration = SyntaxFactory.FieldDeclaration(
                varDeclaration.WithVariables(SyntaxFactory.SingletonSeparatedList(varDeclarator)))
                .WithModifiers(SyntaxFactory.TokenList(AccessibilityToSyntaxToken(accessibility).ToArray()));

            return fieldDeclaration;
        }

        public static LocalDeclarationStatementSyntax DeclareLocalVariable(Type variableType, string name,
            ExpressionSyntax initValue = null,
            VariableDeclarationType variableDeclarationType = VariableDeclarationType.ExplicitType)
        {
            VariableDeclaratorSyntax varDeclarator = SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(name));

            if (initValue != null)
                varDeclarator = varDeclarator.WithInitializer(SyntaxFactory.EqualsValueClause(initValue));

            VariableDeclarationSyntax varDeclaration = variableType == null || variableDeclarationType == VariableDeclarationType.InferredType
                ? SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                : SyntaxFactory.VariableDeclaration(variableType.ToTypeSyntax());
            varDeclaration = varDeclaration.WithVariables(SyntaxFactory.SingletonSeparatedList(varDeclarator));

            return SyntaxFactory.LocalDeclarationStatement(varDeclaration);
        }

        public static LocalDeclarationStatementSyntax DeclareLocalVariable(string typeName, string variableName,
            ExpressionSyntax initValue = null,
            VariableDeclarationType variableDeclarationType = VariableDeclarationType.ExplicitType)
        {
            VariableDeclaratorSyntax varDeclarator = SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(variableName));

            if (initValue != null)
                varDeclarator = varDeclarator.WithInitializer(SyntaxFactory.EqualsValueClause(initValue));

            VariableDeclarationSyntax varDeclaration = variableDeclarationType == VariableDeclarationType.InferredType
                ? SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                : SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName(typeName));
            varDeclaration = varDeclaration.WithVariables(SyntaxFactory.SingletonSeparatedList(varDeclarator));

            return SyntaxFactory.LocalDeclarationStatement(varDeclaration);
        }

        public static MethodDeclarationSyntax DeclareMethod(string name, AccessibilityFlags accessibility,
            Type returnValueType)
        {
            return SyntaxFactory.MethodDeclaration(
                TypeSystem.BuildTypeSyntax(returnValueType),
                SyntaxFactory.Identifier(name))
                .WithModifiers(SyntaxFactory.TokenList(AccessibilityToSyntaxToken(accessibility).ToArray()));
        }

        public static IEnumerable<SyntaxToken> AccessibilityToSyntaxToken(AccessibilityFlags accessibility)
        {
            var modifiers = new List<SyntaxToken>();

            if (accessibility.HasFlag(AccessibilityFlags.Public))
                modifiers.Add(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
            else if (accessibility.HasFlag(AccessibilityFlags.Protected))
                modifiers.Add(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword));
            else if (accessibility.HasFlag(AccessibilityFlags.Private))
                modifiers.Add(SyntaxFactory.Token(SyntaxKind.PrivateKeyword));

            if (accessibility.HasFlag(AccessibilityFlags.Override))
                modifiers.Add(SyntaxFactory.Token(SyntaxKind.OverrideKeyword));
            else if (accessibility.HasFlag(AccessibilityFlags.Static))
                modifiers.Add(SyntaxFactory.Token(SyntaxKind.StaticKeyword));

            return modifiers;
        }

        public static MemberAccessExpressionSyntax MemberReference(SyntaxNode leftHand, string name)
        {
            return SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                leftHand as ExpressionSyntax,
                SyntaxFactory.IdentifierName(name ?? ""));
        }

        public static SyntaxNode ArgumentReference(string name)
        {
            return SyntaxFactory.IdentifierName(name);
        }

        public static IdentifierNameSyntax LocalVariableReference(string name)
        {
            return SyntaxFactory.IdentifierName(name);
        }

        public static LiteralExpressionSyntax EmptyStringLiteralExpression()
        {
            return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(string.Empty));
        }

        internal static ObjectCreationExpressionSyntax CreateConstantInitializationExpression(object value, Type generatedType)
        {
            var x = float.NaN;
            var y = float.NaN;
            var z = float.NaN;
            var w = float.NaN;

            if (value is Vector2 vector2)
            {
                x = vector2.x;
                y = vector2.y;
            }
            else if (value is Vector3 vector3)
            {
                x = vector3.x;
                y = vector3.y;
                z = vector3.z;
            }
            else if (value is Vector4 vector4)
            {
                x = vector4.x;
                y = vector4.y;
                z = vector4.z;
                w = vector4.w;
            }
            else if (value is Quaternion quaternion)
            {
                x = quaternion.x;
                y = quaternion.y;
                z = quaternion.z;
                w = quaternion.w;
            }
            else if (value is Color color)
            {
                x = color.r;
                y = color.g;
                z = color.b;
                w = color.a;
            }

            var argumentSyntaxList = new List<SyntaxNodeOrToken>();
            var arguments = new List<float> { x, y, z, w }.Where(arg => !float.IsNaN(arg)).ToList();
            for (var index = 0; index < arguments.Count; index++)
            {
                var argument = arguments[index];
                argumentSyntaxList.Add(SyntaxFactory.Argument(
                    SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal(argument))));

                if (index < arguments.Count - 1)
                    argumentSyntaxList.Add(SyntaxFactory.Token(SyntaxKind.CommaToken));
            }

            ObjectCreationExpressionSyntax vectorSyntaxNode = SyntaxFactory.ObjectCreationExpression(
                SyntaxFactory.IdentifierName(generatedType.Name))
                .WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SeparatedList<ArgumentSyntax>(argumentSyntaxList)));

            return vectorSyntaxNode;
        }

        public static SyntaxNode BinaryOperator(BinaryOperatorKind operatorKind, SyntaxNode left, SyntaxNode right)
        {
            return SyntaxFactory.ParenthesizedExpression(SyntaxFactory.BinaryExpression(operatorKind.ToSyntaxKind(),
                left as ExpressionSyntax,
                right as ExpressionSyntax));
        }

        public static SyntaxNode UnaryOperator(UnaryOperatorKind kind, SyntaxNode reference)
        {
            if (reference == null)
                return SyntaxFactory.EmptyStatement();

            SyntaxNode statement;
            var operatorKind = kind.ToSyntaxKind();

            if (kind == UnaryOperatorKind.PostDecrement || kind == UnaryOperatorKind.PostIncrement)
            {
                statement = SyntaxFactory.PostfixUnaryExpression(operatorKind, reference as ExpressionSyntax);
            }
            else
            {
                statement = SyntaxFactory.PrefixUnaryExpression(operatorKind, reference as ExpressionSyntax);
            }

            return statement;
        }

        public static IfStatementSyntax IfStatement(SyntaxNode condition, BlockSyntax thenBlock, BlockSyntax elseBlock)
        {
            if (!elseBlock.Statements.Any())
                return SyntaxFactory.IfStatement(condition as ExpressionSyntax, thenBlock);

            return SyntaxFactory.IfStatement(condition as ExpressionSyntax, thenBlock).WithElse(SyntaxFactory.ElseClause(elseBlock));
        }

        public static AssignmentExpressionSyntax Assignment(SyntaxNode reference, SyntaxNode value)
        {
            return SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                reference as ExpressionSyntax,
                value as ExpressionSyntax);
        }

        public static ExpressionSyntax MethodInvocation(string methodName, ExpressionSyntax instance, IEnumerable<ArgumentSyntax> argumentList, IEnumerable<TypeSyntax> typeArgumentList)
        {
            var sepList = SyntaxFactory.SeparatedList(argumentList);

            SimpleNameSyntax method;

            if (typeArgumentList != null && typeArgumentList.Any())
            {
                method = SyntaxFactory.GenericName(
                    SyntaxFactory.Identifier(methodName))
                    .WithTypeArgumentList(
                        SyntaxFactory.TypeArgumentList(
                            SyntaxFactory.SeparatedList(typeArgumentList)));
            }
            else
                method = SyntaxFactory.IdentifierName(SyntaxFactory.Identifier(methodName));

            ExpressionSyntax memberAccessExpression = instance == null
                ? (ExpressionSyntax)(method)
                : SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                instance,
                method);

            ExpressionSyntax invocationSyntax = SyntaxFactory.InvocationExpression(memberAccessExpression)
                .WithArgumentList(SyntaxFactory.ArgumentList(sepList));

            return invocationSyntax;
        }

        public static SyntaxNode MethodInvocation(string name, MethodBase methodInfo, SyntaxNode instance, List<ArgumentSyntax> argumentList, TypeArgumentListSyntax typeArgumentList)
        {
            var sepList = SyntaxFactory.SeparatedList(argumentList);

            if (methodInfo == null)
                return SyntaxFactory.EmptyStatement();

            var propertyInfos = methodInfo.DeclaringType?.GetProperties();
            var isSetAccessor = propertyInfos?.FirstOrDefault(prop => prop.GetSetMethod() == methodInfo);
            var isGetAccessor = propertyInfos?.FirstOrDefault(prop => prop.GetGetMethod() == methodInfo);
            bool isProperty = isSetAccessor != null || isGetAccessor != null;
            var methodName = methodInfo.Name;
            if (isProperty)
            {
                methodName = name.Substring(4);
            }

            bool isGenericMethod = methodInfo.IsGenericMethod;
            ExpressionSyntax finalExpressionSyntax = null;
            ExpressionSyntax memberAccessExpression = instance == null ? SyntaxFactory.IdentifierName(methodInfo.DeclaringType?.Name) : instance as ExpressionSyntax;
            if (!isProperty)
            {
                if (methodInfo.IsConstructor)
                    finalExpressionSyntax = SyntaxFactory.ObjectCreationExpression(
                        TypeSystem.BuildTypeSyntax(methodInfo.DeclaringType))
                        .WithArgumentList(SyntaxFactory.ArgumentList(sepList));
                else
                {
                    SimpleNameSyntax genericName = isGenericMethod
                        ? (SimpleNameSyntax)SyntaxFactory.GenericName(
                        SyntaxFactory.Identifier(methodName))
                            .WithTypeArgumentList(typeArgumentList)
                        : SyntaxFactory.IdentifierName(methodName);

                    InvocationExpressionSyntax invocation = SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            memberAccessExpression,
                            genericName
                        )
                    );
                    finalExpressionSyntax = invocation
                        .WithArgumentList(SyntaxFactory.ArgumentList(sepList));
                }
            }
            else
            {
                if (isGetAccessor != null)
                {
                    if (isGetAccessor.GetIndexParameters().Length > 0)
                    {
                        finalExpressionSyntax = SyntaxFactory.ElementAccessExpression(
                            memberAccessExpression,
                            SyntaxFactory.BracketedArgumentList(sepList));
                    }
                    else
                        finalExpressionSyntax = SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            memberAccessExpression,
                            SyntaxFactory.IdentifierName(methodName));
                }
                else if (isSetAccessor != null)
                {
                    ExpressionSyntax left;
                    if (isSetAccessor.GetIndexParameters().Length > 0)
                    {
                        left = SyntaxFactory.ElementAccessExpression(memberAccessExpression, SyntaxFactory.BracketedArgumentList(
                            SyntaxFactory.SeparatedList(argumentList.Take(argumentList.Count - 1))
                        ));
                    }
                    else
                        left = SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            memberAccessExpression,
                            SyntaxFactory.IdentifierName(methodName));

                    finalExpressionSyntax = SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        left,
                        argumentList.Last().Expression);
                }
            }

            return finalExpressionSyntax;
        }

        public static object GetDefault(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        public static SyntaxNode DeclareLoopCollectionVariable(SyntaxNode collectionNode, string collectionName)
        {
            return SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(
                    SyntaxFactory.IdentifierName("var"))
                    .WithVariables(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(
                                SyntaxFactory.Identifier(collectionName))
                                .WithInitializer(
                                    SyntaxFactory.EqualsValueClause(collectionNode as ExpressionSyntax)))));
        }

        public static SyntaxNode GetProperty(RoslynTranslator translator, IPortModel instancePortModel,
            params string[] members)
        {
            ExpressionSyntax instance = instancePortModel.Connected
                ? translator.BuildPort(instancePortModel).FirstOrDefault() as ExpressionSyntax
                : SyntaxFactory.ThisExpression();

            return GetProperty(instance, members);
        }

        public static SyntaxNode GetProperty(ExpressionSyntax instance, params string[] members)
        {
            instance = members.Aggregate(instance, (current, t) =>
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    current,
                    SyntaxFactory.IdentifierName(t)));

            return instance.NormalizeWhitespace();
        }

        public static SyntaxNode SetProperty(RoslynTranslator translator, AssignmentKind kind,
            IPortModel instancePortModel, IPortModel valuePortModel, params string[] members)
        {
            ExpressionSyntax instance = instancePortModel.Connected
                ? translator.BuildPort(instancePortModel).FirstOrDefault() as ExpressionSyntax
                : SyntaxFactory.ThisExpression();
            ExpressionSyntax value = translator.BuildPort(valuePortModel).FirstOrDefault() as ExpressionSyntax;
            return SetProperty(kind, instance, value, members);
        }

        public static SyntaxNode SetProperty(AssignmentKind kind, ExpressionSyntax instance, ExpressionSyntax value,
            params string[] members)
        {
            instance = members.Aggregate(instance, (current, t) =>
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    current,
                    SyntaxFactory.IdentifierName(t)));

            return SyntaxFactory.ExpressionStatement(
                SyntaxFactory.AssignmentExpression(
                    kind.ToSyntaxKind(),
                    instance,
                    value))
                .NormalizeWhitespace();
        }

        public static SyntaxNode SwitchStatement(ExpressionSyntax switchEvaluatedExpression, params SwitchSectionSyntax[] switchSectionSyntaxes)
        {
            return SyntaxFactory.SwitchStatement(switchEvaluatedExpression).WithSections(SyntaxFactory.List(switchSectionSyntaxes));
        }
    }
}
