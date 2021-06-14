using System;

namespace UnityEngine.GraphToolsFoundation.CommandStateObserver
{
    [Serializable]
    class UndoState : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField]
        SerializedReferenceDictionary<string, string> m_SerializedStates;

        public State State { get; set; }

        public UndoState()
        {
            m_SerializedStates = new SerializedReferenceDictionary<string, string>();
        }

        public void OnBeforeSerialize()
        {
            m_SerializedStates.Clear();
            State?.SerializeForUndo(m_SerializedStates);
        }

        public void OnAfterDeserialize()
        {
            State?.DeserializeFromUndo(m_SerializedStates);
            m_SerializedStates.Clear();
        }

        public void OnValidate()
        {
            State?.ValidateAfterDeserialize();
        }
    }
}
