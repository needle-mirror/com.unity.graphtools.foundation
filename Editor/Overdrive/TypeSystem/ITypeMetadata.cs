using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface ITypeMetadata
    {
        TypeHandle TypeHandle { get; }
        string FriendlyName { get; }
        string Namespace { get; }
        bool IsEnum { get; }
        bool IsClass { get; }
        bool IsValueType { get; }
        bool IsAssignableFrom(ITypeMetadata metadata);
        bool IsAssignableFrom(Type type);
        bool IsAssignableTo(Type type);
        bool IsSuperclassOf(Type type);
        bool IsSubclassOf(Type type);
    }
}
