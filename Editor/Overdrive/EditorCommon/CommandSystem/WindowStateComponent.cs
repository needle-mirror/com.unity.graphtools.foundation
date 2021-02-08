using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    [Serializable]
    public class WindowStateComponent : ViewStateComponent
    {
        [SerializeField]
        OpenedGraph m_CurrentGraph;

        [SerializeField]
        OpenedGraph m_LastOpenedGraph;

        [SerializeField]
        List<OpenedGraph> m_SubGraphStack;

        public OpenedGraph CurrentGraph
        {
            get => m_CurrentGraph;
            set
            {
                if (!string.IsNullOrEmpty(m_CurrentGraph.GraphAssetModelPath))
                    m_LastOpenedGraph = m_CurrentGraph;

                m_CurrentGraph = value;
            }
        }

        public OpenedGraph LastOpenedGraph => m_LastOpenedGraph;

        public IReadOnlyList<OpenedGraph> SubGraphStack => m_SubGraphStack;

        public WindowStateComponent()
        {
            m_SubGraphStack = new List<OpenedGraph>();
        }

        public void PushCurrentGraph()
        {
            m_SubGraphStack.Add(m_CurrentGraph);
        }

        public void TruncateHistory(int length)
        {
            m_SubGraphStack.RemoveRange(length, m_SubGraphStack.Count - length);
        }

        public void ClearHistory()
        {
            m_SubGraphStack.Clear();
        }
    }
}
