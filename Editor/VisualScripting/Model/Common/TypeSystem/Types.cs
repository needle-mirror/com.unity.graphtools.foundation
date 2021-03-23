using System;
using JetBrains.Annotations;
using UnityEngine;

namespace UnityEditor.VisualScripting.Model
{
    /** Types of unary operators supported
    * */
    public enum UnaryOperatorKind
    {
        Minus,
        PostDecrement,
        PostIncrement,
        LogicalNot,
    }

    /** Types of binary operators supported
    * */
    public enum BinaryOperatorKind
    {
        Equals,
        NotEqual,
        Add,
        Subtract,
        BitwiseAnd,
        BitwiseOr,
        Divide,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        LogicalAnd,
        LogicalOr,
        Xor,
        Modulo,
        Multiply,
        AddAssignment,
    }

    /** Event and field accessibility modifier
    * */
    [Serializable]
    [PublicAPI]
    [Flags]
    public enum AccessibilityFlags
    {
        Default = 0,       // No particular accessibility specified, ie void Thing();
        Public = 1,
        Protected = 2,
        Private = 4,
        Static = 8,
        Override = 16
    }
}
