using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    class GraphViewUndoRedoSelection : ScriptableObject, IGraphViewSelection, ISerializationCallbackReceiver
    {
        [SerializeField]
        int m_Version;

        [SerializeField]
        string[] m_SelectedElementsArray;

        [NonSerialized]
        HashSet<string> m_SelectedElements = new HashSet<string>();

        public int Version
        {
            get => m_Version;
            set => m_Version = value;
        }

        public HashSet<string> SelectedElements => m_SelectedElements;

        public void OnBeforeSerialize()
        {
            if (m_SelectedElements.Count == 0)
                return;

            m_SelectedElementsArray = new string[m_SelectedElements.Count];

            m_SelectedElements.CopyTo(m_SelectedElementsArray);
        }

        public void OnAfterDeserialize()
        {
            m_SelectedElements.Clear();

            if (m_SelectedElementsArray == null || m_SelectedElementsArray.Length == 0)
                return;

            foreach (string guid in m_SelectedElementsArray)
            {
                m_SelectedElements.Add(guid);
            }
        }
    }
}
