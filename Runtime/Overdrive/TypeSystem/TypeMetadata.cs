using System;

namespace UnityEngine.GraphToolsFoundation.Overdrive
{
    public class TypeMetadata : ITypeMetadata
    {
        readonly Type m_Type;

        public TypeMetadata(TypeHandle typeHandle, Type type)
        {
            TypeHandle = typeHandle;
            m_Type = type;
        }

        public TypeHandle TypeHandle { get; }

        public string FriendlyName => m_Type.FriendlyName();
        public string Namespace => m_Type.Namespace ?? string.Empty;
        public bool IsEnum => m_Type.IsEnum;
        public bool IsClass => m_Type.IsClass;
        public bool IsValueType => m_Type.IsValueType;

        public bool IsAssignableFrom(ITypeMetadata metadata) => metadata.IsAssignableTo(m_Type);
        public bool IsAssignableFrom(Type type) => m_Type.IsAssignableFrom(type);
        public bool IsAssignableTo(Type t) => t.IsAssignableFrom(m_Type);
        public bool IsSubclassOf(Type t) => m_Type.IsSubclassOf(t);
        public bool IsSuperclassOf(Type t) => t.IsSubclassOf(m_Type);
    }
}
