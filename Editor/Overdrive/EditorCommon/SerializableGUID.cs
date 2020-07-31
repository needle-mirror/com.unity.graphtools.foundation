using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    [StructLayout(LayoutKind.Explicit)]
    [Serializable]
    //[MovedFrom(false, "UnityEditor.VisualScripting.Editor", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    [MovedFrom(false, "UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting")]
    public struct SerializableGUID : IEquatable<SerializableGUID>
    {
        public static SerializableGUID FromParts(ulong a, ulong b) => new SerializableGUID { m_Value0 = a, m_Value1 = b};

        public bool Equals(SerializableGUID other)
        {
            return m_Value0 == other.m_Value0 && m_Value1 == other.m_Value1;
        }

        public override bool Equals(object obj)
        {
            return obj is SerializableGUID other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (m_Value0.GetHashCode() * 397) ^ m_Value1.GetHashCode();
            }
        }

        public static bool operator==(SerializableGUID left, SerializableGUID right)
        {
            return left.Equals(right);
        }

        public static bool operator!=(SerializableGUID left, SerializableGUID right)
        {
            return !left.Equals(right);
        }

        public void ToParts(out ulong a, out ulong b)
        {
            a = m_Value0;
            b = m_Value1;
        }

        [FieldOffset(0)]
        GUID m_GUID;

        [SerializeField]
        [FieldOffset(0)]
        ulong m_Value0;
        [SerializeField]
        [FieldOffset(8)]
        ulong m_Value1;

        public GUID GUID
        {
            get => m_GUID;
            set => m_GUID = value;
        }

        public override string ToString()
        {
            return m_GUID.ToString();
        }

        public static implicit operator GUID(SerializableGUID sGuid) => sGuid.m_GUID;
        public static implicit operator SerializableGUID(GUID guid) => new SerializableGUID{m_GUID = guid};
    }
}
