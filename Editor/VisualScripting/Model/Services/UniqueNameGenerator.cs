using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UnityEditor.VisualScripting.Model.Services
{
    static class UniqueNameGenerator
    {
        public static string CreateUniqueVariableName(Microsoft.CodeAnalysis.SyntaxTree syntaxTree, string baseName)
        {
            var contextNode = syntaxTree.GetRoot();

            var symbols = contextNode.DescendantNodes().OfType<MethodDeclarationSyntax>().Select(n => n.Identifier.ValueText).ToList();
            symbols.AddRange(contextNode.DescendantNodes().OfType<LocalDeclarationStatementSyntax>().Select(n => n.Declaration.Variables.FirstOrDefault().Identifier.ValueText).ToList());
            symbols.AddRange(contextNode.DescendantNodes().OfType<FieldDeclarationSyntax>().Select(n => n.Declaration.Variables.FirstOrDefault().Identifier.ValueText).ToList());
            symbols.AddRange(contextNode.DescendantNodes().OfType<ParameterSyntax>().Select(n => n.Identifier.ValueText).ToList());

            var existingNames = new HashSet<string>(symbols);
            return baseName.GetUniqueName(existingNames);
        }

        internal static string GetUniqueName(this string name, HashSet<string> existingNames)
        {
            int index = 2;
            string basename = name;
            while (existingNames.Contains(name))
                name = $"{basename}_{index++}";
            return name;
        }
    }
}
