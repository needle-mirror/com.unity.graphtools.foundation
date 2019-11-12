using System;
using Microsoft.CodeAnalysis.CSharp;
using UnityEditor.VisualScripting.Model;

namespace VisualScripting.Model.Common.Extensions
{
    public static class OperatorExtensions
    {
        public static SyntaxKind ToSyntaxKind(this BinaryOperatorKind kind)
        {
            switch (kind)
            {
                case BinaryOperatorKind.Equals: return SyntaxKind.EqualsExpression;
                case BinaryOperatorKind.NotEqual: return SyntaxKind.NotEqualsExpression;
                case BinaryOperatorKind.Add: return SyntaxKind.AddExpression;
                case BinaryOperatorKind.Subtract: return SyntaxKind.SubtractExpression;
                case BinaryOperatorKind.BitwiseAnd: return SyntaxKind.BitwiseAndExpression;
                case BinaryOperatorKind.BitwiseOr: return SyntaxKind.BitwiseOrExpression;
                case BinaryOperatorKind.Divide: return SyntaxKind.DivideExpression;
                case BinaryOperatorKind.GreaterThan: return SyntaxKind.GreaterThanExpression;
                case BinaryOperatorKind.GreaterThanOrEqual: return SyntaxKind.GreaterThanOrEqualExpression;
                case BinaryOperatorKind.LessThan: return SyntaxKind.LessThanExpression;
                case BinaryOperatorKind.LessThanOrEqual: return SyntaxKind.LessThanOrEqualExpression;
                case BinaryOperatorKind.LogicalAnd: return SyntaxKind.LogicalAndExpression;
                case BinaryOperatorKind.LogicalOr: return SyntaxKind.LogicalOrExpression;
                case BinaryOperatorKind.Xor: return SyntaxKind.ExclusiveOrExpression;
                case BinaryOperatorKind.Modulo: return SyntaxKind.ModuloExpression;
                case BinaryOperatorKind.Multiply: return SyntaxKind.MultiplyExpression;
                case BinaryOperatorKind.AddAssignment: return SyntaxKind.AddAssignmentExpression;
                default:
                    throw new InvalidOperationException("cannot translate " + kind + " to dotNET backend");
            }
        }

        public static SyntaxKind ToSyntaxKind(this UnaryOperatorKind kind)
        {
            switch (kind)
            {
                case UnaryOperatorKind.LogicalNot: return SyntaxKind.LogicalNotExpression;
                case UnaryOperatorKind.Minus: return SyntaxKind.UnaryMinusExpression;
                case UnaryOperatorKind.PostIncrement: return SyntaxKind.PostIncrementExpression;
                case UnaryOperatorKind.PostDecrement: return SyntaxKind.PostDecrementExpression;
                default:
                    throw new InvalidOperationException("cannot translate " + kind + " to dotNET backend");
            }
        }

        public enum NicifyBinaryOperationKindType
        {
            String,
            CapitalizedString,
            Symbol
        }

        public static string NicifyBinaryOperationKindName(this BinaryOperatorKind operatorKind, NicifyBinaryOperationKindType nicifyType)
        {
            switch (operatorKind)
            {
                case BinaryOperatorKind.Equals:
                    switch (nicifyType)
                    {
                        case NicifyBinaryOperationKindType.CapitalizedString: return "Equals";
                        case NicifyBinaryOperationKindType.String: return "equals";
                        case NicifyBinaryOperationKindType.Symbol: return "==";
                    }
                    break;
                case BinaryOperatorKind.NotEqual:
                    switch (nicifyType)
                    {
                        case NicifyBinaryOperationKindType.CapitalizedString: return "Does Not Equal";
                        case NicifyBinaryOperationKindType.String: return "does not equal";
                        case NicifyBinaryOperationKindType.Symbol: return "!=";
                    }
                    break;
                case BinaryOperatorKind.GreaterThan:
                    switch (nicifyType)
                    {
                        case NicifyBinaryOperationKindType.CapitalizedString: return "Is Greater Than";
                        case NicifyBinaryOperationKindType.String: return "is greater than";
                        case NicifyBinaryOperationKindType.Symbol: return ">";
                    }
                    break;
                case BinaryOperatorKind.GreaterThanOrEqual:
                    switch (nicifyType)
                    {
                        case NicifyBinaryOperationKindType.CapitalizedString: return "Is Greater Than Or Equals";
                        case NicifyBinaryOperationKindType.String: return "is greater than or equals";
                        case NicifyBinaryOperationKindType.Symbol: return ">=";
                    }
                    break;
                case BinaryOperatorKind.LessThan:
                    switch (nicifyType)
                    {
                        case NicifyBinaryOperationKindType.CapitalizedString: return "Is Lesser Than";
                        case NicifyBinaryOperationKindType.String: return "is lesser than";
                        case NicifyBinaryOperationKindType.Symbol: return "<";
                    }
                    break;
                case BinaryOperatorKind.LessThanOrEqual:
                    switch (nicifyType)
                    {
                        case NicifyBinaryOperationKindType.CapitalizedString: return "Is Lesser Than Or Equals";
                        case NicifyBinaryOperationKindType.String: return "is lesser than or equals";
                        case NicifyBinaryOperationKindType.Symbol: return "<=";
                    }
                    break;
            }

            throw new ArgumentException($"Unable to provide a nice name for binary operator kind {operatorKind}");
        }
    }
}
