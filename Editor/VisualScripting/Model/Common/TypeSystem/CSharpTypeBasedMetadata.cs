using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEditor.VisualScripting.Model.Stencils;

namespace UnityEditor.VisualScripting.Model
{
    public class CSharpTypeBasedMetadata : ITypeMetadata
    {
        readonly CSharpTypeSerializer m_Serializer;
        readonly GraphContext m_MemberConstrainer;
        readonly Type m_Type;

        public delegate CSharpTypeBasedMetadata FactoryMethod(TypeHandle typeHandle, Type type);
        public CSharpTypeBasedMetadata(GraphContext memberConstrainer,
                                       TypeHandle typeHandle, Type type)
        {
            TypeHandle = typeHandle;
            m_Serializer = memberConstrainer.CSharpTypeSerializer;
            m_MemberConstrainer = memberConstrainer;
            m_Type = type;
        }

        public TypeHandle TypeHandle { get; }

        public string FriendlyName => m_Type.FriendlyName();
        public string Name => m_Type.Name;
        public string Namespace => m_Type.Namespace ?? string.Empty;
        public bool IsEnum => m_Type.IsEnum;
        public bool IsClass => m_Type.IsClass;
        public bool IsValueType => m_Type.IsValueType;

        public IEnumerable<TypeHandle> GenericArguments => m_Type.GenericTypeArguments
        .Select(t => m_Serializer.GenerateTypeHandle(t));
        public List<MemberInfoValue> PublicMembers => MemberInfoDtos(BindingFlags.Public | BindingFlags.Instance);
        public List<MemberInfoValue> NonPublicMembers => MemberInfoDtos(BindingFlags.NonPublic | BindingFlags.Instance);

        public bool IsAssignableFrom(ITypeMetadata metadata) => metadata.IsAssignableTo(m_Type);
        public bool IsAssignableFrom(Type type) => m_Type.IsAssignableFrom(type);

        public bool IsAssignableTo(ITypeMetadata metadata) => metadata.IsAssignableFrom(m_Type);
        public bool IsAssignableTo(Type t) => t.IsAssignableFrom(m_Type);

        public bool IsSubclassOf(ITypeMetadata metadata) => metadata.IsSuperclassOf(m_Type);
        public bool IsSubclassOf(Type t) => m_Type.IsSubclassOf(t);

        public bool IsSuperclassOf(ITypeMetadata metadata) => metadata.IsSubclassOf(m_Type);
        public bool IsSuperclassOf(Type t) => t.IsSubclassOf(m_Type);

        List<MemberInfoValue> MemberInfoDtos(BindingFlags flags)
        {
            if (m_Type.IsEnum)
                return new List<MemberInfoValue>();

            var membersToParse = Fields(flags).Concat(Properties(flags));
            return FilterMembers(membersToParse)
                .OrderBy(m => m.Name)
                .ToList();
        }

        IEnumerable<MemberInfo> Fields(BindingFlags flags)
        {
            return m_Type.GetFields(flags);
        }

        IEnumerable<MemberInfo> Properties(BindingFlags flags)
        {
            return m_Type.GetProperties(flags).Where(p => !PropertyRequireParameters(p));
        }

        IEnumerable<MemberInfoValue> FilterMembers(IEnumerable<MemberInfo> members)
        {
            foreach (MemberInfo memberInfo in members)
            {
                if (IsObsolete(memberInfo) || IsCompilerGenerated(memberInfo))
                    continue;

                var memberInfoDto = memberInfo.ToMemberInfoValue(m_Serializer);
                if (MemberAllowed(memberInfoDto))
                    yield return memberInfoDto;
            }
        }

        static bool PropertyRequireParameters(PropertyInfo propertyInfo)
        {
            return propertyInfo.GetIndexParameters().Length > 0;
        }

        bool MemberAllowed(MemberInfoValue value)
        {
            return m_MemberConstrainer.MemberAllowed(value);
        }

        static bool IsObsolete(MemberInfo propertyInfo)
        {
            return propertyInfo.GetCustomAttribute<ObsoleteAttribute>() != null;
        }

        static bool IsCompilerGenerated(MemberInfo mi)
        {
            return mi.IsDefined(typeof(CompilerGeneratedAttribute), false);
        }
    }
}
