using System;
using System.Collections.Generic;

namespace UnityEditor.EditorCommon.Utility
{
    static class HashSetExtension
    {
        internal static void AddRange<T>(this HashSet<T> set, IEnumerable<T> entriesToAdd)
        {
            foreach (var entry in entriesToAdd)
            {
                set.Add(entry);
            }
        }
    }
}
