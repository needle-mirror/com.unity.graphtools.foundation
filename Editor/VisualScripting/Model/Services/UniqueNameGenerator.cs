using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.VisualScripting.Model.Services
{
    static class UniqueNameGenerator
    {
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
