using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public static class StringExtensions
    {
        public static string Nicify(this string value)
        {
            return ObjectNames.NicifyVariableName(value);
        }

        static readonly Regex k_NonLegitChars = new Regex(@"[^\s\w]", RegexOptions.Compiled);

        // Convert to Unity approved property name format.
        public static string ToUnityNameFormat(this string userName)
        {
            string newName = string.Concat(k_NonLegitChars.Replace(userName, "")
                .Split(new[] {'_', ' '}, StringSplitOptions.RemoveEmptyEntries)
                .Select((s, i) => (i == 0 ? char.ToLower(s[0]) : char.ToUpper(s[0])) + s.Substring(1, s.Length - 1)));

            if (string.IsNullOrWhiteSpace(newName))
            {
                return string.Empty;
            }

            // Modify newName if they are c# keyword or
            // if the newName that starts with a number
            if (newName.IsCSharpKeyword() | char.IsNumber(newName[0]))
            {
                string firstLetter = newName.Substring(0, 1).ToUpper();
                newName = "my" + firstLetter + newName.Remove(0, 1);
            }

            return newName;
        }

        static readonly Regex k_CodifyRegex = new Regex("[^a-zA-Z0-9]", RegexOptions.Compiled);

        public static string CodifyString(string str)
        {
            return k_CodifyRegex.Replace(str, "_");
        }

        public static bool IsCSharpKeyword(this string name)
        {
            switch (name)
            {
                case "bool":
                case "byte":
                case "sbyte":
                case "short":
                case "ushort":
                case "int":
                case "uint":
                case "long":
                case "ulong":
                case "double":
                case "float":
                case "decimal":
                case "string":
                case "char":
                case "object":
                case "typeof":
                case "sizeof":
                case "null":
                case "true":
                case "false":
                case "if":
                case "else":
                case "while":
                case "for":
                case "foreach":
                case "do":
                case "switch":
                case "case":
                case "default":
                case "lock":
                case "try":
                case "throw":
                case "catch":
                case "finally":
                case "goto":
                case "break":
                case "continue":
                case "return":
                case "public":
                case "private":
                case "internal":
                case "protected":
                case "static":
                case "readonly":
                case "sealed":
                case "const":
                case "new":
                case "override":
                case "abstract":
                case "virtual":
                case "partial":
                case "ref":
                case "out":
                case "in":
                case "where":
                case "params":
                case "this":
                case "base":
                case "namespace":
                case "using":
                case "class":
                case "struct":
                case "interface":
                case "delegate":
                case "checked":
                case "get":
                case "set":
                case "add":
                case "remove":
                case "operator":
                case "implicit":
                case "explicit":
                case "fixed":
                case "extern":
                case "event":
                case "enum":
                case "unsafe":
                    return true;
                default:
                    return false;
            }
        }
    }
}
