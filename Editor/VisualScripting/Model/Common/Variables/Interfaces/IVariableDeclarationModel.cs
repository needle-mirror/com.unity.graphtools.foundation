using System;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEditor.VisualScripting.Model.Translators;

namespace UnityEditor.VisualScripting.Model
{
    public interface IVariableDeclarationModel : IGraphElementModelWithGuid
    {
        string Title { get; }
        string Name { get; }
        VariableType VariableType { get; }
        string VariableName { get; }
        TypeHandle DataType { get; }
        bool IsExposed { get; }
        IConstantNodeModel InitializationModel { get; }
        IHasVariableDeclaration Owner { get; }
        ModifierFlags Modifiers { get;  }
        string Tooltip { get; }
    }

    [PublicAPI]
    // ReSharper disable once InconsistentNaming
    public static class IVariableDeclarationModelExtensions
    {
        public static FieldDeclarationSyntax DeclareField(this IVariableDeclarationModel decl,
            RoslynTranslator translator, bool useInitialization = true)
        {
            var declaration = decl.DeclareVariable(translator, useInitialization, true);
            var modifier = SyntaxFactory.Token(decl.IsExposed ? SyntaxKind.PublicKeyword : SyntaxKind.PrivateKeyword);
            var fieldDeclaration = SyntaxFactory.FieldDeclaration(declaration)
                .WithModifiers(SyntaxFactory.TokenList(modifier));

            return fieldDeclaration;
        }

        public static StatementSyntax DeclareLocalVariable(this IVariableDeclarationModel decl,
            RoslynTranslator translator, bool useInitialization = true)
        {
            var variableDeclarationSyntax = decl.DeclareVariable(translator, useInitialization, false);
            return SyntaxFactory.LocalDeclarationStatement(variableDeclarationSyntax);
        }

        public static StatementSyntax DeclareLoopCountVariable(this IVariableDeclarationModel decl,
            ExpressionSyntax collectionNodeModel, string collectionName, RoslynTranslator translator)
        {
            if (collectionNodeModel == null)
                return decl.DeclareLocalVariable(translator);

            return SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(
                    SyntaxFactory.PredefinedType(
                        SyntaxFactory.Token(SyntaxKind.IntKeyword)))
                    .WithVariables(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(
                                SyntaxFactory.Identifier(decl.Name))
                                .WithInitializer(
                                    SyntaxFactory.EqualsValueClause(
                                        SyntaxFactory.MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            string.IsNullOrEmpty(collectionName)
                                            ? collectionNodeModel
                                            : SyntaxFactory.IdentifierName(collectionName),
                                            SyntaxFactory.IdentifierName("Count")))))));
        }

        public static StatementSyntax DeclareLoopIndexVariable(this IVariableDeclarationModel decl, int startingValue = 0)
        {
            return SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(
                    SyntaxFactory.PredefinedType(
                        SyntaxFactory.Token(SyntaxKind.IntKeyword)))
                    .WithVariables(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(
                                SyntaxFactory.Identifier(decl.Name))
                                .WithInitializer(
                                    SyntaxFactory.EqualsValueClause(
                                        SyntaxFactory.LiteralExpression(
                                            SyntaxKind.NumericLiteralExpression,
                                            SyntaxFactory.Literal(startingValue)))))));
        }

        static VariableDeclarationSyntax DeclareVariable(this IVariableDeclarationModel decl, RoslynTranslator translator,
            bool useInitialization, bool isField)
        {
            bool canBeImplicitlyTyped = !isField;
            bool initialized = false;

            var varDeclarator = SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(decl.VariableName));
            if (useInitialization && translator.Stencil.RequiresInitialization(decl))
            {
                if (decl.InitializationModel != null)
                {
                    var expression = translator.BuildNode(decl.InitializationModel).SingleOrDefault() as ExpressionSyntax;
                    varDeclarator = varDeclarator.WithInitializer(SyntaxFactory.EqualsValueClause(expression));
                    initialized = true;
                }
            }

            VariableDeclarationSyntax varDeclaration;
            if (canBeImplicitlyTyped && initialized)
            {
                varDeclaration = SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"));
            }
            else
            {
                varDeclaration = SyntaxFactory.VariableDeclaration(decl.DataType.ToTypeSyntax(translator.Stencil));
            }

            return varDeclaration.WithVariables(SyntaxFactory.SingletonSeparatedList(varDeclarator));
        }
    }
}
