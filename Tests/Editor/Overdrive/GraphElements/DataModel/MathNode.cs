using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    public abstract class MathNode : ScriptableObject
    {
        // Implicit port multi output.

        [SerializeField]
        private MathNodeID m_NodeID = MathNodeID.empty;

        public MathBook mathBook { get; set; }

        public MathNodeID nodeID
        {
            get { return m_NodeID; }
        }

        public Vector2 m_Position;
        public MathNodeID m_GroupNodeID = MathNodeID.empty;

        protected MathNode()
        {
            m_NodeID = MathNodeID.NewID();
        }

        public MathNodeID RewriteID()
        {
            m_NodeID = MathNodeID.NewID();
            return m_NodeID;
        }

        public virtual void RemapReferences(Dictionary<string, string> oldIDNewIDMap)
        {
            RemapID(oldIDNewIDMap, ref m_GroupNodeID);
        }

        public abstract float Evaluate();

        public abstract void ResetConnections();

        static public void RemapID(Dictionary<string, string> oldIDNewIDMap, ref MathNodeID id)
        {
            if (oldIDNewIDMap.ContainsKey(id.ToString()))
            {
                id = new MathNodeID(oldIDNewIDMap[id.ToString()]);
            }
            else
            {
                id = MathNodeID.empty;
            }
        }
    }
}
