using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class Unknown
    {
        Unknown() {}
    }

    public class MissingPort
    {
        MissingPort() {}
    }

    [Serializable]
    //[MovedFrom(false, "UnityEditor.VisualScripting.Model.Stencils", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    [MovedFrom(false, "UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting")]
    [PublicAPI]
    public struct TypeHandle : IEquatable<TypeHandle>, IComparable<TypeHandle>
    {
        //TODO figure how to implement
        public static TypeHandle MissingType { get; } = TypeSerializer.GenerateCustomTypeHandle("__MISSINGTYPE");
        public static TypeHandle Unknown { get; }  = TypeSerializer.GenerateCustomTypeHandle(typeof(Unknown), "__UNKNOWN");
        public static TypeHandle MissingPort { get; }  = TypeSerializer.GenerateTypeHandle(typeof(MissingPort));
        public static TypeHandle Bool { get; } = TypeSerializer.GenerateTypeHandle(typeof(bool));
        public static TypeHandle Void { get; } = TypeSerializer.GenerateTypeHandle(typeof(void));
        public static TypeHandle Char { get; } = TypeSerializer.GenerateTypeHandle(typeof(char));
        public static TypeHandle Double { get; } = TypeSerializer.GenerateTypeHandle(typeof(double));
        public static TypeHandle Float { get; } = TypeSerializer.GenerateTypeHandle(typeof(float));
        public static TypeHandle Int { get; } = TypeSerializer.GenerateTypeHandle(typeof(int));
        public static TypeHandle UInt { get; } = TypeSerializer.GenerateTypeHandle(typeof(uint));
        public static TypeHandle Long { get; } = TypeSerializer.GenerateTypeHandle(typeof(long));
        public static TypeHandle Object { get; } = TypeSerializer.GenerateTypeHandle(typeof(object));
        public static TypeHandle GameObject { get; } = TypeSerializer.GenerateTypeHandle(typeof(GameObject));
        public static TypeHandle String { get; } = TypeSerializer.GenerateTypeHandle(typeof(string));
        public static TypeHandle Vector2 { get; } = TypeSerializer.GenerateTypeHandle(typeof(Vector2));
        public static TypeHandle Vector3 { get; } = TypeSerializer.GenerateTypeHandle(typeof(Vector3));
        public static TypeHandle Vector4 { get; } = TypeSerializer.GenerateTypeHandle(typeof(Vector4));
        public static TypeHandle Quaternion { get; } = TypeSerializer.GenerateTypeHandle(typeof(Quaternion));

        public bool IsValid => !string.IsNullOrEmpty(Identification);

        public string Identification;

        internal TypeHandle(string identification)
        {
            Identification = identification;
            m_Name = null;
        }

        string m_Name;
        public string Name => m_Name ?? (m_Name = Resolve().Name);

        public bool Equals(TypeHandle other)
        {
            return string.Equals(Identification, other.Identification);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is TypeHandle th && Equals(th);
        }

        public override int GetHashCode()
        {
            // ReSharper disable once NonReadonlyMemberInGetHashCode
            return Identification?.GetHashCode() ?? 0;
        }

        public override string ToString()
        {
            return $"TypeName:{Identification}";
        }

        public static bool operator==(TypeHandle left, TypeHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator!=(TypeHandle left, TypeHandle right)
        {
            return !left.Equals(right);
        }

        public int CompareTo(TypeHandle other)
        {
            return string.Compare(Identification, other.Identification, StringComparison.Ordinal);
        }

        public Type Resolve()
        {
            return TypeSerializer.ResolveType(this);
        }

        public ITypeMetadata GetMetadata(Stencil stencil)
        {
            return GetMetadata(stencil.GraphContext.TypeMetadataResolver);
        }

        public ITypeMetadata GetMetadata(ITypeMetadataResolver resolver)
        {
            return resolver.Resolve(this);
        }

        public bool IsAssignableFrom(TypeHandle other, Stencil stencil)
        {
            var selfMetadata = GetMetadata(stencil);
            var otherMetadata = other.GetMetadata(stencil);
            return selfMetadata.IsAssignableFrom(otherMetadata);
        }
    }

    [PublicAPI]
    public static class TypeHandleExtensions
    {
        public static TypeHandle GenerateTypeHandle(this Type t)
        {
            Assert.IsNotNull(t);
            return TypeSerializer.GenerateTypeHandle(t);
        }
    }
}
