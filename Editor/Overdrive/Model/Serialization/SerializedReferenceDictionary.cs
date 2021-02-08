using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Dictionary that safely survives serialization
    /// Done so by waiting for access before deserializing Values
    /// This solves issues with SerializeReference not being ready to be accessed during OnAfterDeserialize()
    /// </summary>
    /// <typeparam name="TKey">Type of key</typeparam>
    /// <typeparam name="TValue">Type of value</typeparam>
    [Serializable]
    internal class SerializedReferenceDictionary<TKey, TValue>: IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField]
        List<TKey> m_KeyList;

        [SerializeReference]
        List<TValue> m_ValueList;

        private Dictionary<TKey, TValue> m_Dictionary;

        public bool IsValid => m_KeyList != null && m_ValueList != null;

        public SerializedReferenceDictionary(int capacity = 0)
        {
            m_Dictionary = new Dictionary<TKey, TValue>(capacity);
            m_KeyList = new List<TKey>(capacity);
            m_ValueList = new List<TValue>(capacity);
        }

        public static SerializedReferenceDictionary<TKey, TValue> FromLists(IReadOnlyList<TKey> keys, IReadOnlyList<TValue> values)
        {
            return new SerializedReferenceDictionary<TKey, TValue>(keys ?? new List<TKey>(), values);
        }

        private SerializedReferenceDictionary(IReadOnlyList<TKey> keys, IReadOnlyList<TValue> values)
            : this(keys.Count)
        {
            m_Dictionary.DeserializeDictionaryFromLists(keys, values);
        }

        private Dictionary<TKey, TValue> GetSafeDictionary()
        {
            if (m_Dictionary == null)
            {
                if (!IsValid)
                {
                    m_KeyList = new List<TKey>();
                    m_ValueList = new List<TValue>();
                }
                m_Dictionary = new Dictionary<TKey, TValue>(m_KeyList.Count);
                m_Dictionary.DeserializeDictionaryFromLists(m_KeyList, m_ValueList);
            }

            return m_Dictionary;
        }

        #region ISerializationCallbackReceiver implementation
        public void OnBeforeSerialize()
        {
            m_Dictionary?.SerializeDictionaryToLists(out m_KeyList, out m_ValueList);
        }

        public void OnAfterDeserialize()
        {
            m_Dictionary = null; // force rebuild dictionary, as references in Values will be lost
        }

        #endregion ISerializationCallbackReceiver implementation

        #region IDictionary<TKey, TValue> implementation
        public TValue this[TKey key]
        {
            get => GetSafeDictionary()[key];
            set => GetSafeDictionary()[key] = value;
        }
        public ICollection<TKey> Keys => GetSafeDictionary().Keys;
        public ICollection<TValue> Values => GetSafeDictionary().Values;
        public bool ContainsKey(TKey key) => GetSafeDictionary().ContainsKey(key);
        public void Add(TKey key, TValue value) => GetSafeDictionary().Add(key, value);
        public bool Remove(TKey key) => GetSafeDictionary().Remove(key);
        public bool TryGetValue(TKey key, out TValue value) => GetSafeDictionary().TryGetValue(key, out value);

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => GetSafeDictionary().GetEnumerator();

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => GetSafeDictionary().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetSafeDictionary().GetEnumerator();

        public void Add(KeyValuePair<TKey, TValue> item) => GetSafeDictionary().Add(item.Key, item.Value);

        public void Clear() => GetSafeDictionary().Clear();

        public bool Contains(KeyValuePair<TKey, TValue> item) => GetSafeDictionary().Contains(item);

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            foreach (var kvi in GetSafeDictionary().Select((kv, i) => (kv, i)))
            {
                array[arrayIndex + kvi.i] = kvi.kv;
            }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item) => GetSafeDictionary().Remove(item.Key);

        public int Count => GetSafeDictionary().Count;

        public bool IsReadOnly => false;
        #endregion IDictionary<TKey, TValue> implementation

        #region IReadOnlyDictionary<TKey, TValue> implementation
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;
        #endregion IReadOnlyDictionary<TKey, TValue> implementation
    }
}