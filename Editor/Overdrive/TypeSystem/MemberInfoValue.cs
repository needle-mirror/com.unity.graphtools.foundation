using System;
using System.Reflection;
using JetBrains.Annotations;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public struct MemberInfoValue : IEquatable<MemberInfoValue>
    {
        public readonly string Name;
        readonly TypeHandle m_UnderlyingType;
        readonly TypeHandle m_ReflectedType;
        readonly MemberTypes m_MemberType;

        public MemberInfoValue(TypeHandle reflectedType, TypeHandle underlyingType, string name, MemberTypes memberType)
        {
            Name = name;
            m_UnderlyingType = underlyingType;
            m_ReflectedType = reflectedType;
            m_MemberType = memberType;
        }

        public bool Equals(MemberInfoValue other)
        {
            return m_ReflectedType.Equals(other.m_ReflectedType) &&
                string.Equals(Name, other.Name) &&
                m_UnderlyingType.Equals(other.m_UnderlyingType) &&
                m_MemberType == other.m_MemberType;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is MemberInfoValue other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = m_ReflectedType.GetHashCode();
                hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ m_UnderlyingType.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)m_MemberType;
                return hashCode;
            }
        }

        public static bool operator==(MemberInfoValue left, MemberInfoValue right)
        {
            return left.Equals(right);
        }

        public static bool operator!=(MemberInfoValue left, MemberInfoValue right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return Name;
        }
    }

    [PublicAPI]
    public static class MemberInfoValueExtensions
    {
        public static MemberInfoValue ToMemberInfoValue(this MemberInfo mi)
        {
            return new MemberInfoValue(
                TypeSerializer.GenerateTypeHandle(mi.ReflectedType),
                TypeSerializer.GenerateTypeHandle(mi.GetUnderlyingType()),
                mi.Name,
                mi.MemberType);
        }
    }
}
