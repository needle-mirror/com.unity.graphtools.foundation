using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnityEditor.EditorCommon.Extensions;
using UnityEditor.VisualScripting.Model.Translators;
using UnityEngine;

namespace UnityEditor.VisualScripting.Model
{
    public static class TypeSystem
    {
        static readonly Regex k_CodifyRegex = new Regex("[^a-zA-Z0-9]", RegexOptions.Compiled);

        static IEnumerable<BinaryOperatorKind> s_StringOperators;
        static IEnumerable<BinaryOperatorKind> s_BooleanOperators;

        static IEnumerable<BinaryOperatorKind> StringOperators => s_StringOperators
        ?? (s_StringOperators = new List<BinaryOperatorKind>
        {
            BinaryOperatorKind.Equals,
            BinaryOperatorKind.NotEqual,
            BinaryOperatorKind.Add,
            BinaryOperatorKind.AddAssignment
        });

        static IEnumerable<BinaryOperatorKind> BooleanOperators => s_BooleanOperators
        ?? (s_BooleanOperators = new List<BinaryOperatorKind>
        {
            BinaryOperatorKind.Equals,
            BinaryOperatorKind.NotEqual,
            BinaryOperatorKind.LogicalAnd,
            BinaryOperatorKind.LogicalOr
        });

        public static MethodInfo GetMethod(Type parentType, string methodName, bool isStatic)
        {
            var bindingFlags = BindingFlags.Public;
            if (isStatic)
                bindingFlags |= BindingFlags.Static;
            else
                bindingFlags |= BindingFlags.Instance;

            var mi = parentType.GetMethods(bindingFlags).FirstOrDefault(m => m.Name == methodName);
            if (mi == null)
            {
                mi = GetUserExtensionMethods(parentType).FirstOrDefault(x => x.Name == methodName);
                if (mi == null)
                {
                    int index = methodName.IndexOf("<");
                    var truncatedName = (index > 0 ? methodName.Substring(0, index) : methodName);
                    mi = GetUserExtensionMethods(parentType).FirstOrDefault(x => x.Name == truncatedName);
                }
            }

            return mi;
        }

        public static string CodifyString(string str)
        {
            return k_CodifyRegex.Replace(str, "_");
        }

        // Create a dictionary of every method in a class tagged with TAttribute, grouped by the type of their first parameter type
        // [MyAttr] class Foo { public void A(float); public void B(int); public void C(float); public void D() }
        // GetExtensionMethods<MyAttrAttribute>() =>  { float => {A, C}, int => B }
        public static Dictionary<Type, List<MethodInfo>> GetExtensionMethods<TAttribute>(IEnumerable<Assembly> assemblies) where TAttribute : Attribute
        {
            Type GetMethodFirstParameterType(MethodInfo m) => m.GetParameters()[0].ParameterType.IsArray ? m.GetParameters()[0].ParameterType.GetElementType() : m.GetParameters()[0].ParameterType;
            return TypeCache.GetTypesWithAttribute<TAttribute>()
                .Where(t => assemblies.Contains(t.Assembly) && t.IsClass)
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly))
                .Where(m => m.GetParameters().Length > 0)
                .GroupBy(GetMethodFirstParameterType)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        public static Dictionary<Type, List<MethodInfo>> GetExtensionMethods(IEnumerable<Assembly> assemblies)
        {
            return GetExtensionMethods<ExtensionAttribute>(assemblies);
        }

        static IEnumerable<MethodInfo> GetUserExtensionMethods(Type type)
        {
            var userAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.FullName.IndexOf("Assembly-CSharp") >= 0);
            var extensions = GetExtensionMethods(userAssemblies);

            return extensions[type];
        }

        public static List<MethodInfo> GetBinaryOperators(BinaryOperatorKind kind, Type left, Type right)
        {
            var res = new List<MethodInfo>();
            string compiledName = GetBinaryOperatorCompiledName(kind);
            if (left != null)
                res.AddRange(left.GetMethods(BindingFlags.Public | BindingFlags.Static).Where(m =>
                {
                    if (!m.IsSpecialName || m.Name != compiledName)
                        return false;
                    var parameters = m.GetParameters();
                    if (parameters.Length != 2)
                        return false;
                    return right == null || parameters[1].ParameterType.IsAssignableFrom(right);
                }));
            if (right != null)
                res.AddRange(right.GetMethods(BindingFlags.Public | BindingFlags.Static).Where(m =>
                {
                    if (!m.IsSpecialName || m.Name != compiledName)
                        return false;
                    var parameters = m.GetParameters();
                    if (parameters.Length != 2)
                        return false;
                    return left == null || parameters[0].ParameterType.IsAssignableFrom(left);
                }));
            return res;
        }

        static string GetBinaryOperatorCompiledName(BinaryOperatorKind kind)
        {
            switch (kind)
            {
                case BinaryOperatorKind.Equals: return "op_Equality";
                case BinaryOperatorKind.NotEqual: return "op_Inequality";
                case BinaryOperatorKind.Add: return "op_Addition";
                case BinaryOperatorKind.Subtract: return "op_Subtraction";
                case BinaryOperatorKind.BitwiseAnd: return "op_BitwiseAnd";
                case BinaryOperatorKind.BitwiseOr: return "op_BitwiseOr";
                case BinaryOperatorKind.Divide: return "op_Division";
                case BinaryOperatorKind.GreaterThan: return "op_GreaterThan";
                case BinaryOperatorKind.GreaterThanOrEqual: return "op_GreaterThanOrEqual";
                case BinaryOperatorKind.LessThan: return "op_LessThan";
                case BinaryOperatorKind.LessThanOrEqual: return "op_LessThanOrEqual";
                case BinaryOperatorKind.LogicalAnd: return "op_LogicalAnd";
                case BinaryOperatorKind.LogicalOr: return "op_LogicalOr";
                case BinaryOperatorKind.Xor: return "op_ExclusiveOr";
                case BinaryOperatorKind.Modulo: return "op_Modulus";
                case BinaryOperatorKind.Multiply: return "op_Multiply";
                case BinaryOperatorKind.AddAssignment: return "op_AdditionAssignment";
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        static string GetUnaryOperatorCompiledName(UnaryOperatorKind kind)
        {
            switch (kind)
            {
                case UnaryOperatorKind.Minus: return "op_UnaryNegation";
                case UnaryOperatorKind.PostDecrement: return "op_Decrement";
                case UnaryOperatorKind.PostIncrement: return "op_Increment";
                case UnaryOperatorKind.LogicalNot: return "op_LogicalNot";
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        public static int HashMethodSignature(MethodBase meh)
        {
            int hash = 17 ^ meh.Name.GetHashCode();
            foreach (var p in meh.GetParameters())
            {
                hash = hash * 31 + (p.Name.GetHashCode() ^ p.ParameterType.Name.GetHashCode());
            }

            return hash;
        }

        public static TypeSyntax BuildTypeSyntax(Type type)
        {
            return type.ToTypeSyntax();
        }

        public static IEnumerable<BinaryOperatorKind> GetOverloadedBinaryOperators(Type type)
        {
            if (type.IsNumeric())
            {
                return Enum.GetValues(typeof(BinaryOperatorKind)).Cast<BinaryOperatorKind>();
            }

            if (type == typeof(string))
            {
                return StringOperators;
            }

            if (type == typeof(bool))
            {
                return BooleanOperators;
            }

            var kinds = new List<BinaryOperatorKind>();
            MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);

            if (typeof(Unknown) == type)
            {
                foreach (BinaryOperatorKind kind in Enum.GetValues(typeof(BinaryOperatorKind)))
                    kinds.Add(kind);
            }
            else
            {
                foreach (BinaryOperatorKind kind in Enum.GetValues(typeof(BinaryOperatorKind)))
                {
                    try
                    {
                        string compiledName = GetBinaryOperatorCompiledName(kind);
                        var infos = methods.Where(m =>
                        {
                            if (!m.IsSpecialName || m.Name != compiledName)
                                return false;

                            return m.GetParameters().Length == 2;
                        });

                        if (infos.Any())
                        {
                            kinds.Add(kind);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.Log(e);
                    }
                }
            }

            return kinds;
        }

        public static bool IsBinaryOperationPossible(Type a, Type b, BinaryOperatorKind kind)
        {
            if ((a.IsEnum || b.IsEnum) &&
                (kind == BinaryOperatorKind.Add
                 || kind == BinaryOperatorKind.Divide
                 || kind == BinaryOperatorKind.Modulo
                 || kind == BinaryOperatorKind.Multiply
                 || kind == BinaryOperatorKind.Subtract
                 || kind == BinaryOperatorKind.AddAssignment))
                return false;

            if (a.IsNumeric() && b.IsNumeric())
                return true;

            string compiledName = GetBinaryOperatorCompiledName(kind);
            var methods = a.GetMethods(BindingFlags.Public | BindingFlags.Static).Where(m =>
            {
                if (!m.IsSpecialName || m.Name != compiledName)
                    return false;

                return m.GetParameters().Length == 2;
            });

            if (methods.Select(method => method.GetParameters()[1].ParameterType)
                .Any(secondParamType => secondParamType.IsAssignableFrom(b)
                    || secondParamType.HasNumericConversionTo(b)))
                return true;

            methods = b.GetMethods(BindingFlags.Public | BindingFlags.Static).Where(m =>
            {
                if (!m.IsSpecialName || m.Name != compiledName)
                    return false;

                return m.GetParameters().Length == 2;
            });

            return methods.Select(method => method.GetParameters()[0].ParameterType)
                .Any(firstParamType => firstParamType.IsAssignableFrom(a)
                    || firstParamType.HasNumericConversionTo(a));
        }

        public static IEnumerable<UnaryOperatorKind> GetOverloadedUnaryOperators(Type type)
        {
            if (type == typeof(bool))
            {
                return new List<UnaryOperatorKind> { UnaryOperatorKind.LogicalNot };
            }

            if (type.IsNumeric())
            {
                return new List<UnaryOperatorKind>
                {
                    UnaryOperatorKind.Minus,
                    UnaryOperatorKind.PostDecrement,
                    UnaryOperatorKind.PostIncrement
                };
            }

            var kinds = new List<UnaryOperatorKind>();
            MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);

            if (typeof(Unknown) == type)
            {
                foreach (UnaryOperatorKind kind in Enum.GetValues(typeof(UnaryOperatorKind)))
                    kinds.Add(kind);
            }
            else
            {
                foreach (UnaryOperatorKind kind in Enum.GetValues(typeof(UnaryOperatorKind)))
                {
                    try
                    {
                        string compiledName = GetUnaryOperatorCompiledName(kind);
                        var infos = methods.Where(m =>
                        {
                            if (!m.IsSpecialName || m.Name != compiledName)
                                return false;

                            return m.GetParameters().Length == 1;
                        });

                        if (infos.Any())
                        {
                            kinds.Add(kind);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.Log(e);
                    }
                }
            }

            return kinds;
        }
    }
}
