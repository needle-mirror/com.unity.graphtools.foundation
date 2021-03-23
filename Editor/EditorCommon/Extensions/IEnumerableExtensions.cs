using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace UnityEditor.EditorCommon.Extensions
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class IEnumerableExtensions
    {
        internal static IEnumerable<T> OfExactType<T>(this IEnumerable source)
        {
            if (source == null)
                throw new ArgumentException("Must specify a valid source", nameof(source));

            return OfExactTypeIterator<T>(source);
        }

        static IEnumerable<T> OfExactTypeIterator<T>(IEnumerable source)
        {
            return source.OfType<T>().Where(obj => obj.GetType() == typeof(T));
        }

        public static int IndexOf<T>(this IEnumerable<T> source, T element)
        {
            if (source is IList<T> list)
                return list.IndexOf(element);

            int i = 0;
            foreach (var x in source)
            {
                if (Equals(x, element))
                    return i;
                i++;
            }

            return -1;
        }

        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source, IEqualityComparer<T> comparer = null)
        {
            return new HashSet<T>(source, comparer);
        }
    }
}
