using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    public class MathBook : ScriptableObject
    {
        [SerializeField]
        private List<MathNode> m_Nodes;
        static private List<MathNode> s_EmptyNodes = new List<MathNode>();

        [SerializeField]
        private MathBookInputOutputContainer m_InputOutputs;

        public List<MathNode> nodes { get { return m_Nodes != null ? m_Nodes : s_EmptyNodes; } }
        public MathBookInputOutputContainer inputOutputs
        {
            get
            {
                if (m_InputOutputs == null)
                {
                    m_InputOutputs = CreateInstance<MathBookInputOutputContainer>();
                    m_InputOutputs.name = "Input Output Container";
                    m_InputOutputs.mathBook = this;
                    AssetDatabase.AddObjectToAsset(m_InputOutputs, this);
                }
                return m_InputOutputs;
            }
        }

        [SerializeField]
        private List<MathStickyNote> m_StickyNotes;

        public ReadOnlyCollection<MathStickyNote> stickyNotes
        {
            get
            {
                if (m_StickyNotes == null)
                    m_StickyNotes = new List<MathStickyNote>();
                return m_StickyNotes.AsReadOnly();
            }
        }

        [SerializeField]
        private List<MathPlacemat> m_Placemats;

        public ReadOnlyCollection<MathPlacemat> placemats
        {
            get
            {
                if (m_Placemats == null)
                    m_Placemats = new List<MathPlacemat>();
                return m_Placemats.AsReadOnly();
            }
        }

        public void Add(MathStickyNote sticky)
        {
            if (sticky == null)
                return;

            if (m_StickyNotes == null)
                m_StickyNotes = new List<MathStickyNote>();

            m_StickyNotes.Add(sticky);
            sticky.mathBook = this;
        }

        public void Remove(MathStickyNote sticky)
        {
            if (sticky == null || m_StickyNotes == null)
                return;
            m_StickyNotes.Remove(sticky);
            sticky.mathBook = null;
        }

        public void Add(MathPlacemat mat)
        {
            if (mat == null)
                return;

            if (m_Placemats == null)
                m_Placemats = new List<MathPlacemat>();

            m_Placemats.Add(mat);
            mat.mathBook = this;
        }

        public void Remove(MathPlacemat mat)
        {
            if (mat == null || m_Placemats == null)
                return;
            m_Placemats.Remove(mat);
            mat.mathBook = null;
        }

        public void Add(MathNode node)
        {
            if (node == null)
                return;

            if (m_Nodes == null)
                m_Nodes = new List<MathNode>();

            m_Nodes.Add(node);
            node.mathBook = this;
        }

        public void Remove(MathNode node)
        {
            if (node == null || m_Nodes == null)
                return;
            m_Nodes.Remove(node);
            node.mathBook = null;
        }

        public void Clear()
        {
            m_Nodes?.Clear();
            m_Placemats?.Clear();
        }

        public void OnEnable()
        {
            if (m_Nodes == null)
                m_Nodes = new List<MathNode>();
            else
            {
                foreach (MathNode node in m_Nodes.Where(n => n != null))
                {
                    node.mathBook = this;
                }
            }

            if (m_InputOutputs != null)
                m_InputOutputs.mathBook = this;
        }

        public MathNode Get(MathNodeID id)
        {
            return m_Nodes != null ? m_Nodes.Find(node => node.nodeID.Equals(id)) : null;
        }
    }
}
