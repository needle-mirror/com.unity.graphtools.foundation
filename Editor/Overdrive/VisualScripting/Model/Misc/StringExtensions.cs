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
    }
}
