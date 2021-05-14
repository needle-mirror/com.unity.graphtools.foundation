using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;

namespace UnityEngine.GraphToolsFoundation.CommandStateObserver
{
    public static class SerializationHelpers
    {
        public static void SerializeDictionaryToLists<K, V>(IReadOnlyDictionary<K, V> dic, out List<K> keys, out List<V> values)
        {
            if (dic == null)
            {
                keys = new List<K>();
                values = new List<V>();
                return;
            }

            keys = dic.Keys.ToList();
            values = dic.Values.ToList();
        }

        public static void DeserializeDictionaryFromLists<K, V>(ref Dictionary<K, V> dic, IReadOnlyList<K> keys, IReadOnlyList<V> values)
        {
            int numKeys = keys?.Count ?? 0;

            if (dic == null)
                dic = new Dictionary<K, V>(keys.Count);

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
