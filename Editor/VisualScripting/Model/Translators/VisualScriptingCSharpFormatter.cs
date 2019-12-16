using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UnityEditor.VisualScripting.Model.Translators
{
    class VisualScriptingCSharpFormatter : CSharpSyntaxRewriter
    {
        // set to 0 to force line breaks everywhere
        const int k_LineLength = 120;
        const int k_IndentSize = 4;

        int m_CurrentIndent;

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            m_CurrentIndent += k_IndentSize;
            var visited = base.VisitClassDeclaration(node);
            m_CurrentIndent -= k_IndentSize;
            return visited;
        }

        public override SyntaxNode VisitStructDeclaration(StructDeclarationSyntax node)
        {
            m_CurrentIndent += k_IndentSize;
            var visited = base.VisitStructDeclaration(node);
            m_CurrentIndent -= k_IndentSize;
            return visited;
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            m_CurrentIndent += k_IndentSize;
            var visited = base.VisitMethodDeclaration(node);
            m_CurrentIndent -= k_IndentSize;
            return visited;
        }

        // Entities.Foreach((Entity e, ...) => {}): base indent set by VisitBlock, add 1 level for the lambda parameters
        public override SyntaxNode VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
        {
            m_CurrentIndent += k_IndentSize;
            var visited = base.VisitParenthesizedLambdaExpression(node);
            m_CurrentIndent -= k_IndentSize;
            return visited;
        }

        // indent is set by the containing VisitMethodDeclaration or VisitBlock+VisitParenthesizedLambda
        public override SyntaxNode VisitParameterList(ParameterListSyntax node)
        {
            var separatedSyntaxList = node.Parameters;
            if (!LineSpanNeedsWrapping(node, separatedSyntaxList))
                return base.VisitParameterList(node);

            node = node
                .WithOpenParenToken(SyntaxFactory.Token(SyntaxKind.OpenParenToken).WithTrailingTrivia(SyntaxFactory.TriviaList(SyntaxFactory.LineFeed)))
                .WithParameters(FormatList(separatedSyntaxList));
            var visited = base.VisitParameterList(node);

            return visited;
        }

        public override SyntaxNode VisitBlock(BlockSyntax node)
        {
            var lineSpan = node.SyntaxTree.GetLineSpan(node.Span);
            int previousIndent = m_CurrentIndent;
            m_CurrentIndent = lineSpan.StartLinePosition.Character + k_IndentSize;
            var visited = base.VisitBlock(node);
            m_CurrentIndent = previousIndent;
            return visited;
        }

        // indent is set by the containing VisitBlock
        public override SyntaxNode VisitTypeArgumentList(TypeArgumentListSyntax node)
        {
            var separatedSyntaxList = node.Arguments;
            if (!LineSpanNeedsWrapping(node, separatedSyntaxList))
                return base.VisitTypeArgumentList(node);

            m_CurrentIndent += k_IndentSize;
            node = node
                .WithLessThanToken(SyntaxFactory.Token(SyntaxKind.LessThanToken).WithTrailingTrivia(SyntaxFactory.TriviaList(SyntaxFactory.LineFeed)))
                .WithArguments(FormatList(separatedSyntaxList));
            var visited = base.VisitTypeArgumentList(node);
            m_CurrentIndent -= k_IndentSize;

            return visited;
        }

        // reference indent is set by the containing VisitBlock, actual indent is set here to support nested calls
        public override SyntaxNode VisitArgumentList(ArgumentListSyntax node)
        {
            var separatedSyntaxList = node.Arguments;
            if (!LineSpanNeedsWrapping(node, separatedSyntaxList))
                return base.VisitArgumentList(node);

            m_CurrentIndent += k_IndentSize;
            node = node
                .WithOpenParenToken(SyntaxFactory.Token(SyntaxKind.OpenParenToken).WithTrailingTrivia(SyntaxFactory.TriviaList(SyntaxFactory.LineFeed)))
                .WithArguments(FormatList(separatedSyntaxList));
            var visited = base.VisitArgumentList(node);
            m_CurrentIndent -= k_IndentSize;

            return visited;
        }

        SeparatedSyntaxList<T> FormatList<T>(SeparatedSyntaxList<T> separatedSyntaxList) where T : SyntaxNode
        {
            return SyntaxFactory.SeparatedList(
                separatedSyntaxList.Select((a, i) => a.WithLeadingTrivia(SyntaxFactory.TriviaList(
                    SyntaxFactory.Whitespace(new string(' ', m_CurrentIndent))))),
                Enumerable.Repeat(
                    SyntaxFactory.Token(SyntaxKind.CommaToken).WithTrailingTrivia(SyntaxFactory.TriviaList(SyntaxFactory.LineFeed)),
                    separatedSyntaxList.Count - 1));
        }

        static bool LineSpanNeedsWrapping<T>(SyntaxNode node, SeparatedSyntaxList<T> separatedSyntaxList) where T : SyntaxNode
        {
            if (separatedSyntaxList.Count == 0)
                return false;

            var lineSpan = node.SyntaxTree.GetLineSpan(node.Span);
            return lineSpan.EndLinePosition.Character >= k_LineLength;
        }
    }
}
