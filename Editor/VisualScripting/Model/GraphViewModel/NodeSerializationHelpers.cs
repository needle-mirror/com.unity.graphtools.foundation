using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.VisualScripting.Model;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEditor.VisualScripting.GraphViewModel
{
    public static class NodeSerializationHelpers
    {
        public static void SerializeDictionaryToLists<K, V>(this IReadOnlyDictionary<K, V> dic, ref List<K> keys, ref List<V> values) where V : INodeModel
        {
            if (dic == null)
                return;
            keys = dic.Keys.ToList();
            values = dic.Values.ToList();
        }

        public static void DeserializeDictionaryFromLists<K, V>(this Dictionary<K, V> dic, IList<K> keys, IList<V> values) where V : INodeModel
        {
            int numKeys = keys?.Count ?? 0;

            dic.Clear();

            if (numKeys != 0)
            {
                Assert.IsNotNull(keys);
                Assert.IsNotNull(values);
                Assert.AreEqual(keys.Count, values.Count);
                for (int i = 0; i < numKeys; i++)
                {
                    if (values[i] != null)
                        dic.Add(keys[i], values[i]);
                }
            }
        }
    }
}
