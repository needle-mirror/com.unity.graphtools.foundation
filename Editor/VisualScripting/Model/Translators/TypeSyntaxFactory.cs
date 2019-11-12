using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UnityEditor.VisualScripting.Model.Translators
{
    public static class TypeSyntaxFactory
    {
        static readonly string k_VSArrayReplacingTypeFriendlyName = GetFriendlyTypeName(typeof(List<>));

        struct TypeToKind
        {
            public Type type;
            public SyntaxKind kind;
        };

        static readonly TypeToKind[] k_TypeKindArray =
        {
            new TypeToKind {type = typeof(void),   kind = SyntaxKind.VoidKeyword},
            new TypeToKind {type = typeof(bool),   kind = SyntaxKind.BoolKeyword},
            new TypeToKind {type = typeof(char),   kind = SyntaxKind.CharKeyword},
            new TypeToKind {type = typeof(int),    kind = SyntaxKind.IntKeyword},
            new TypeToKind {type = typeof(uint),   kind = SyntaxKind.UIntKeyword},
            new TypeToKind {type = typeof(long),   kind = SyntaxKind.LongKeyword},
            new TypeToKind {type = typeof(ulong),  kind = SyntaxKind.ULongKeyword},
            new TypeToKind {type = typeof(byte),   kind = SyntaxKind.ByteKeyword},
            new TypeToKind {type = typeof(sbyte),  kind = SyntaxKind.SByteKeyword},
            new TypeToKind {type = typeof(short),  kind = SyntaxKind.ShortKeyword},
            new TypeToKind {type = typeof(ushort), kind = SyntaxKind.UShortKeyword},
            new TypeToKind {type = typeof(float),  kind = SyntaxKind.FloatKeyword},
            new TypeToKind {type = typeof(double), kind = SyntaxKind.DoubleKeyword},
            new TypeToKind {type = typeof(string), kind = SyntaxKind.StringKeyword},
            new TypeToKind {type = typeof(object), kind = SyntaxKind.ObjectKeyword},
        };

        public static Type KindToType(SyntaxKind kind)
        {
            return k_TypeKindArray.SingleOrDefault(t => t.kind == kind).type;
        }

        public static TypeSyntax ToTypeSyntax(this VSGraphModel graphToConvert)
        {
            return SyntaxFactory.ParseTypeName(graphToConvert.name);
        }

        public static TypeSyntax ToTypeSyntax(this Type typeToConvert)
        {
            TypeSyntax ts;
            if (typeToConvert.IsArray)
            {
                var elementTypeSyntax = typeToConvert.GetElementType().ToTypeSyntax();
                ts = SyntaxFactory.ArrayType(elementTypeSyntax)
                    .WithRankSpecifiers(
                        SyntaxFactory.SingletonList(
                            SyntaxFactory.ArrayRankSpecifier(
                                SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                                    SyntaxFactory.OmittedArraySizeExpression()))));
            }
            else if (typeToConvert.IsGenericType)
            {
                TypeArgumentListSyntax typeArgumentListSyntax = GenerateGenericTypeArgumentList(typeToConvert);

                var genericTypeName = GetFriendlyTypeName(typeToConvert);

                ts = SyntaxFactory.GenericName(SyntaxFactory.Identifier(genericTypeName))
                    .WithTypeArgumentList(typeArgumentListSyntax);
            }
            else
            {
                var roslynPredefKind = KindFromType(typeToConvert);
                if (roslynPredefKind != SyntaxKind.None)
                {
                    ts = SyntaxFactory.PredefinedType(SyntaxFactory.Token(roslynPredefKind));
                }
                else
                {
                    if (typeToConvert.IsNested)
                        ts = SyntaxFactory.QualifiedName(SyntaxFactory.ParseName(typeToConvert.DeclaringType.FullName), SyntaxFactory.IdentifierName(typeToConvert.Name));
                    else
                        ts = SyntaxFactory.ParseTypeName(typeToConvert.FullName);
                }
            }
            return ts;
        }

        static TypeArgumentListSyntax GenerateGenericTypeArgumentList(Type genericType)
        {
            TypeSyntax[] typeSyntaxArguments = genericType.GenericTypeArguments.Select(ToTypeSyntax).ToArray();

            if (typeSyntaxArguments.Length == 0)
            {
                //NOTE A no-argument generic type is usually used for reflection use cases. Should we fail here?
                return SyntaxFactory.TypeArgumentList();
            }

            if (typeSyntaxArguments.Length == 1)
            {
                var singleItemList = SyntaxFactory.SingletonSeparatedList(typeSyntaxArguments[0]);
                return SyntaxFactory.TypeArgumentList(singleItemList);
            }

            // Generic Arguments Length > 1
            {
                //Reason for the formula "Length * 2 - 1":
                //We need to include the commas "in-between" every generic type argument
                //ex: Dictionary<string,int>
                int arraySize = typeSyntaxArguments.Length * 2 - 1;
                SyntaxNodeOrToken[] syntaxNodeOrTokenList = new SyntaxNodeOrToken[arraySize];

                syntaxNodeOrTokenList[0] = typeSyntaxArguments[0];
                for (int i = 1; i < typeSyntaxArguments.Length; i++)
                {
                    syntaxNodeOrTokenList[i * 2 - 1] = SyntaxFactory.Token(SyntaxKind.CommaToken);
                    syntaxNodeOrTokenList[i * 2] = typeSyntaxArguments[i];
                }

                SeparatedSyntaxList<TypeSyntax> separatedList = SyntaxFactory.SeparatedList<TypeSyntax>(syntaxNodeOrTokenList);
                return SyntaxFactory.TypeArgumentList(separatedList);
            }
        }

        static SyntaxKind KindFromType(Type type)
        {
            return k_TypeKindArray.SingleOrDefault(t => t.type == type).kind;
        }

        static string GetFriendlyTypeName(Type typeToConvert)
        {
            string typeName = typeToConvert.Name;
            string cleanedTypeName = typeName.Substring(0, typeName.IndexOf('`'));
            return cleanedTypeName;
        }
    }
}
