using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    [Serializable]
    class GraphToolStateUndo : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField]
        List<string> m_SerializedStateKey;
        [SerializeField]
        List<string> m_SerializedStateData;

        public GraphToolState State { get; set; }

        public GraphToolStateUndo()
        {
            m_SerializedStateKey = new List<string>();
            m_SerializedStateData = new List<string>();
        }

        public void OnBeforeSerialize()
        {
            var serializedState = new Dictionary<string, string>();
            State?.SerializeForUndo(serializedState);
            serializedState.SerializeDictionaryToLists(out m_SerializedStateKey, out m_SerializedStateData);
        }

        public void OnAfterDeserialize()
        {
            var serializedState = new Dictionary<string, string>();
            serializedState.DeserializeDictionaryFromLists(m_SerializedStateKey, m_SerializedStateData);
            State?.DeserializeFromUndo(serializedState);
        }
    }
}
