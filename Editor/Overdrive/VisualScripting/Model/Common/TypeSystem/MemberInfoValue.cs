using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using JetBrains.Annotations;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public struct MemberInfoValue : IEquatable<MemberInfoValue>
    {
        public string Name;
        public TypeHandle UnderlyingType;
        TypeHandle m_ReflectedType;
        MemberTypes m_MemberType;

        public MemberInfoValue(TypeHandle reflectedType, TypeHandle underlyingType, string name, MemberTypes memberType)
        {
            Name = name;
            UnderlyingType = underlyingType;
            m_ReflectedType = reflectedType;
            m_MemberType = memberType;
        }

        public bool Equals(MemberInfoValue other)
        {
            return m_ReflectedType.Equals(other.m_ReflectedType) &&
                string.Equals(Name, other.Name) &&
                UnderlyingType.Equals(other.UnderlyingType) &&
                m_MemberType == other.m_MemberType;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is MemberInfoValue other && Equals(other);
        }

        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = m_ReflectedType.GetHashCode();
                hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ UnderlyingType.GetHashCode();
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
        public static MemberInfoValue ToMemberInfoValue(this MemberInfo mi, Stencil stencil)
        {
            return new MemberInfoValue(
                mi.DeclaringType.GenerateTypeHandle(stencil),
                mi.GetUnderlyingType().GenerateTypeHandle(stencil),
                mi.Name,
                mi.MemberType);
        }

        public static MemberInfoValue ToMemberInfoValue(this MemberInfo mi)
        {
            return new MemberInfoValue(
                CSharpTypeSerializer.GenerateTypeHandle(mi.ReflectedType),
                CSharpTypeSerializer.GenerateTypeHandle(mi.GetUnderlyingType()),
                mi.Name,
                mi.MemberType);
        }
    }
}
