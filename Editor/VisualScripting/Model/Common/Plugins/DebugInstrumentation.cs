using System;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEditor.VisualScripting.Model.Translators;
using UnityEngine;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Object = UnityEngine.Object;
// ReSharper disable InvalidXmlDocComment

namespace UnityEditor.VisualScripting.Plugins
{
    /// <summary>
    /// Instruments the AST such that when the code runs, it sends instrumentation data back to Unity for live display / debugging.
    /// </summary>
    /// <remarks>
    /// The instrumentation data is the following:
    ///<para>
    /// 1- On nodes that "compute a value", we wrap the call in a RecordValue&lt;T&gt; call that will send the computed value back to Unity for display along
    ///    the edge of SemanticModel node. This also registers an execution steps.
    ///    For instance, this:
    /// <code>
    ///    var a = f(10 + 2);
    /// </code>
    ///    will become
    /// <code>
    ///    var a = RecordValue(f(RecordValue(RecordValue(10, _) + RecordValue(2, _), _), _);
    /// </code>
    ///</para>
    ///<para>
    /// 2- All statements in a block might be void-ish statements (void function call, return , ...). In that case, we can't
    /// use RecordValue. If the statement is a return, it's impossible to register the step after its execution, so it needs
    /// to be done before the call and it must specify the number of expected RecordValue() calls preceding it.
    /// <code>PadAndInsert
    ///   return(10 + 2);
    /// </code>
    ///    will become
    /// <code>
    ///    SetLastCallFrame("return", 3);
    ///    return RecordValue(RecordValue(10, _) + RecordValue(2, _), _), _;
    /// </code>
    /// The first call registers the return step 3 steps from now, which will be filled after that by the 3 RecordValue calls.
    /// See <see cref="UnityEditor.VisualScriptingTests.FrameTests"/> for an example.
    ///</para>
    ///</remarks>
    public static class InstrumentForInEditorDebugging
    {
        public static InvocationExpressionSyntax RecordValue(IdentifierNameSyntax recorderVariableSyntax, ExpressionSyntax expression, Type returnType, NodeModel node)
        {
            SimpleNameSyntax name = IdentifierName("Record");
            ((SerializableGUID)node.Guid).ToParts(out var guid1, out var guid2);
            return InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    recorderVariableSyntax,
                    name))
                    .WithArgumentList(
                ArgumentList(
                    SeparatedList(
                        new[]
                        {
                            Argument(expression),
                            Argument(
                                LiteralExpression(
                                    SyntaxKind.NumericLiteralExpression,
                                    Literal(guid1))),
                            Argument(
                                LiteralExpression(
                                    SyntaxKind.NumericLiteralExpression,
                                    Literal(guid2)))
                        })))
                    // this is critical as it's used to count the number of recorded values when inserting a SetLastCallFrame
                    // leaving the right number of empty values
                    .WithAdditionalAnnotations(new SyntaxAnnotation(Annotations.RecordValueKind));
        }

        public static ExpressionStatementSyntax BuildLastCallFrameExpression(int recordedValuesCount, GUID id, IdentifierNameSyntax recorderVariableSyntax, ExpressionSyntax progressReportingVariableName = null)
        {
            ((SerializableGUID)id).ToParts(out var guid1, out var guid2);

            bool reportProgress = progressReportingVariableName != null;
            var argumentSyntaxes = new ArgumentSyntax[reportProgress ? 4 : 3];

            argumentSyntaxes[0] = Argument(
                LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    Literal(guid1)));
            argumentSyntaxes[1] = Argument(
                LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    Literal(guid2)));
            argumentSyntaxes[2] = Argument(
                LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    Literal(recordedValuesCount)));

            if (reportProgress)
                argumentSyntaxes[3] = Argument(InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        progressReportingVariableName,
                        IdentifierName("GetProgress"))));

            return ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        recorderVariableSyntax,
                        IdentifierName("SetLastCallFrame")))
                    .WithArgumentList(
                    ArgumentList(
                        SeparatedList(
                            argumentSyntaxes))));
        }
    }
}
