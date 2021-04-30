using System.Collections.Generic;

namespace UnityEngine.GraphToolsFoundation.Overdrive
{
    public static class HashSetExtension
    {
        public static void AddRange<T>(this HashSet<T> set, IEnumerable<T> entriesToAdd)
        {
            foreach (var entry in entriesToAdd)
            {
                set.Add(entry);
            }
        }
    }
}
