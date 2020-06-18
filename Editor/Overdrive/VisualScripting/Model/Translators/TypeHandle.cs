using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    [Serializable]
    [MovedFrom(false, "UnityEditor.VisualScripting.Model.Stencils", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    [PublicAPI]
    public struct TypeHandle : IEquatable<TypeHandle>, IComparable<TypeHandle>
    {
        static CSharpTypeSerializer CSharpSerializer { get; } = new CSharpTypeSerializer();

        //TODO figure how to implement
        public static TypeHandle ExecutionFlow { get; } = new TypeHandle("__EXECUTIONFLOW");
        public static TypeHandle MissingType { get; } = new TypeHandle("__MISSINGTYPE");
        public static TypeHandle ThisType { get; }  = new TypeHandle("__THISTYPE");
        public static TypeHandle Unknown { get; }  = new TypeHandle("__UNKNOWN");
        public static TypeHandle MissingPort { get; }  = GenerateTypeHandle(typeof(MissingPort));
        public static TypeHandle Bool { get; } = GenerateTypeHandle(typeof(bool));
        public static TypeHandle Void { get; } = GenerateTypeHandle(typeof(void));
        public static TypeHandle Char { get; } = GenerateTypeHandle(typeof(char));
        public static TypeHandle Double { get; } = GenerateTypeHandle(typeof(double));
        public static TypeHandle Float { get; } = GenerateTypeHandle(typeof(float));
        public static TypeHandle Int { get; } = GenerateTypeHandle(typeof(int));
        public static TypeHandle UInt { get; } = GenerateTypeHandle(typeof(uint));
        public static TypeHandle Long { get; } = GenerateTypeHandle(typeof(long));
        public static TypeHandle Object { get; } = GenerateTypeHandle(typeof(object));
        public static TypeHandle GameObject { get; } = GenerateTypeHandle(typeof(GameObject));
        public static TypeHandle String { get; } = GenerateTypeHandle(typeof(string));
        public static TypeHandle Vector2 { get; } = GenerateTypeHandle(typeof(Vector2));
        public static TypeHandle Vector3 { get; } = GenerateTypeHandle(typeof(Vector3));
        public static TypeHandle Vector4 { get; } = GenerateTypeHandle(typeof(Vector4));
        public static TypeHandle Quaternion { get; } = GenerateTypeHandle(typeof(Quaternion));

        public bool IsValid => !string.IsNullOrEmpty(Identification);

        public string Identification;

        public TypeHandle(string identification)
        {
            Identification = identification;
        }

        [Obsolete("Use TypeHandle.GetMetadata.FriendlyName instead")]
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public string FriendlyName(Stencil stencil)
        {
            if (!string.IsNullOrEmpty(Identification))
                return this.Resolve(stencil).FriendlyName();
            return "";
        }

        public string Name(Stencil stencil)
        {
            return this.Resolve(stencil).Name;
        }

        public bool Equals(TypeHandle other)
        {
            return string.Equals(Identification, other.Identification);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is TypeHandle th && Equals(th);
        }

        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode()
        {
            return Identification != null ? Identification.GetHashCode() : 0;
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

        public IEnumerable<TypeHandle> GetGenericArguments(Stencil stencil)
        {
            foreach (var t in this.Resolve(stencil).GenericTypeArguments)
                yield return t.GenerateTypeHandle(stencil);
        }

        static TypeHandle GenerateTypeHandle(Type type)
        {
            return CSharpTypeSerializer.GenerateTypeHandle(type);
        }

        public int CompareTo(TypeHandle other)
        {
            return string.Compare(Identification, other.Identification, StringComparison.Ordinal);
        }
    }

    [PublicAPI]
    public static class TypeHandleExtensions
    {
        // TODO : Remove this and use GenerateTypeHandle(this Type t)
        public static TypeHandle GenerateTypeHandle(this Type t, Stencil stencil)
        {
            Assert.IsNotNull(t);
            return t.GenerateTypeHandle();
        }

        public static TypeHandle GenerateTypeHandle(this Type t)
        {
            return CSharpTypeSerializer.GenerateTypeHandle(t);
        }

        public static Type Resolve(this TypeHandle th, Stencil stencil)
        {
            return th.Resolve(stencil.GraphContext.CSharpTypeSerializer);
        }

        public static Type Resolve(this TypeHandle th, CSharpTypeSerializer serializer)
        {
            return serializer.ResolveType(th);
        }

        public static ITypeMetadata GetMetadata(this Type t, Stencil stencil)
        {
            return t.GenerateTypeHandle(stencil).GetMetadata(stencil);
        }

        public static ITypeMetadata GetMetadata(this TypeHandle th, Stencil stencil)
        {
            return th.GetMetadata(stencil.GraphContext.TypeMetadataResolver);
        }

        public static ITypeMetadata GetMetadata(this TypeHandle th, ITypeMetadataResolver resolver)
        {
            return resolver.Resolve(th);
        }

        public static ITypeMetadata GetMetadata(this Type t, CSharpTypeSerializer serializer,
            ITypeMetadataResolver resolver)
        {
            return t.GenerateTypeHandle().GetMetadata(resolver);
        }

        public static bool IsAssignableFrom(this TypeHandle self, TypeHandle other, Stencil stencil)
        {
            var selfMetadata = self.GetMetadata(stencil);
            var otherMetadata = other.GetMetadata(stencil);
            return selfMetadata.IsAssignableFrom(otherMetadata);
        }

        public static bool IsSubclassOf(this TypeHandle self, TypeHandle other, Stencil stencil)
        {
            var selfMetadata = self.GetMetadata(stencil);
            var otherMetadata = other.GetMetadata(stencil);
            return selfMetadata.IsSubclassOf(otherMetadata);
        }
    }
}
