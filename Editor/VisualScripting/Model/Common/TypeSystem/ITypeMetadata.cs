using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using UnityEditor.VisualScripting.Model.Stencils;

namespace UnityEditor.VisualScripting.Model
{
    public interface ITypeMetadata
    {
        TypeHandle TypeHandle { get; }
        string FriendlyName { get; }
        string Name { get; }
        string Namespace { get; }
        List<MemberInfoValue> PublicMembers { get; }
        List<MemberInfoValue> NonPublicMembers { get; }
        IEnumerable<TypeHandle> GenericArguments { get; }
        bool IsEnum { get; }
        bool IsClass { get; }
        bool IsValueType { get; }

        bool IsAssignableFrom(ITypeMetadata metadata);
        bool IsAssignableFrom(Type type);

        bool IsAssignableTo(ITypeMetadata metadata);
        bool IsAssignableTo(Type type);

        bool IsSuperclassOf(ITypeMetadata metadata);
        bool IsSuperclassOf(Type type);

        bool IsSubclassOf(ITypeMetadata metadata);
        bool IsSubclassOf(Type type);
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class ITypeMetadataExtensions
    {
        public static IEnumerable<MemberInfoValue> GetMembers(this ITypeMetadata metadata, BindingFlags flags)
        {
            var privateAndPublicFlags = BindingFlags.Public | BindingFlags.NonPublic;
            if ((flags & privateAndPublicFlags) == privateAndPublicFlags)
                return metadata.PublicMembers.Concat(metadata.NonPublicMembers);
            if ((flags & BindingFlags.NonPublic) != 0)
                return metadata.NonPublicMembers;
            //if ((flags & BindingFlags.Public) != 0)
            return metadata.PublicMembers;
        }
    }
}
