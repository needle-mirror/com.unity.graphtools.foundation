using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace UnityEditor.VisualScripting.Editor
{
    [StructLayout(LayoutKind.Explicit)]
    [Serializable]
    public struct SerializableGUID
    {
        public static SerializableGUID FromParts(ulong a, ulong b) => new SerializableGUID { m_Value0 = a, m_Value1 = b};

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
