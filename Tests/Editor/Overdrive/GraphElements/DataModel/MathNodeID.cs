using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    [Serializable]
    public struct MathNodeID : IEquatable<MathNodeID>
    {
        [SerializeField]
        private string m_NodeGuid;

        private static readonly MathNodeID s_Empty = new MathNodeID { m_NodeGuid = "" };

        public MathNode Get(MathBook book)
        {
            if (book == null)
                return null;
            return book.Get(this);
        }

        public void Set(MathNode node)
        {
            m_NodeGuid = node == null ? null : node.nodeID.m_NodeGuid;
        }

        public MathNodeID(string guid)
        {
            m_NodeGuid = guid;
        }

        public static MathNodeID empty { get { return s_Empty; } }

        static public MathNodeID NewID()
        {
            return new MathNodeID { m_NodeGuid = Guid.NewGuid().ToString() };
        }

        public bool Equals(MathNodeID other)
        {
            if (ReferenceEquals(this, other)) return true;
            return m_NodeGuid.Equals(other.m_NodeGuid);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((MathNodeID)obj);
        }

        public static bool operator!=(MathNodeID lhs, MathNodeID rhs)
        {
            return !lhs.Equals(rhs);
        }

        public static bool operator==(MathNodeID lhs, MathNodeID rhs)
        {
            return lhs.Equals(rhs);
        }

        public override int GetHashCode()
        {
            return m_NodeGuid.GetHashCode();
        }

        public override string ToString()
        {
            return m_NodeGuid;
        }
    }
}
