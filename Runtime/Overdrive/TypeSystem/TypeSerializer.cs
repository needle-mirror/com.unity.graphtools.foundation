using System;
using System.Collections.Generic;

namespace UnityEngine.GraphToolsFoundation.Overdrive
{
    public static class TypeSerializer
    {
        static System.Text.RegularExpressions.Regex s_GenericTypeExtractionRegex = new System.Text.RegularExpressions.Regex(@"(?<=\[\[)(.*?)(?=\]\])");

        static List<ValueTuple<string, TypeHandle>> s_CustomIdToTypeHandle = new List<ValueTuple<string, TypeHandle>>();
        static List<ValueTuple<string, Type>> s_CustomIdToType = new List<ValueTuple<string, Type>>();

        internal static Func<string, Type> GetMovedFromType;

        static string Serialize(Type t)
        {
            var mapping = s_CustomIdToType.Find(e => e.Item2 == t);
            if (mapping != default)
            {
                return mapping.Item1;
            }

            return t.AssemblyQualifiedName;
        }

        static Type Deserialize(string serializedType)
        {
            var mapping = s_CustomIdToType.Find(e => e.Item1 == serializedType);
            if (mapping != default)
            {
                return mapping.Item2;
            }

            return GetTypeFromName(serializedType);
        }

        static Type GetTypeFromName(string assemblyQualifiedName)
        {
            Type retType = typeof(Unknown);
            if (!string.IsNullOrEmpty(assemblyQualifiedName))
            {
                var type = Type.GetType(assemblyQualifiedName);
                if (type == null)
                {
                    // Check if the type has moved
                    assemblyQualifiedName = ExtractAssemblyQualifiedName(assemblyQualifiedName, out var isList);
                    var movedType = GetMovedFromType?.Invoke(assemblyQualifiedName);
                    if (movedType != null)
                    {
                        type = movedType;
                        if (isList)
                        {
                            type = typeof(List<>).MakeGenericType(type);
                        }
                    }
                }

                retType = type ?? retType;
            }
            return retType;
        }

        static string ExtractAssemblyQualifiedName(string fullTypeName, out bool isList)
        {
            isList = false;
            if (fullTypeName.StartsWith("System.Collections.Generic.List"))
            {
                fullTypeName = s_GenericTypeExtractionRegex.Match(fullTypeName).Value;
                isList = true;
            }

            // remove the assembly version string
            var versionIdx = fullTypeName.IndexOf(", Version=");
            if (versionIdx > 0)
                fullTypeName = fullTypeName.Substring(0, versionIdx);
            // replace all '+' with '/' to follow the Unity serialization convention for nested types
            fullTypeName = fullTypeName.Replace("+", "/");
            return fullTypeName;
        }

        public static Type ResolveType(TypeHandle th)
        {
            return Deserialize(th.Identification);
        }

        public static TypeHandle GenerateCustomTypeHandle(string uniqueId)
        {
            TypeHandle th;
            var typeHandleMapping = s_CustomIdToTypeHandle.Find(e => e.Item1 == uniqueId);
            if (typeHandleMapping != default)
            {
                Debug.LogWarning(uniqueId + " is already registered in TypeSerializer");
                return typeHandleMapping.Item2;
            }

            th = new TypeHandle(uniqueId);
            s_CustomIdToTypeHandle.Add((uniqueId, th));
            return th;
        }

        public static TypeHandle GenerateCustomTypeHandle(Type t, string customUniqueId)
        {
            TypeHandle th;

            var typeHandleMapping = s_CustomIdToTypeHandle.Find(e => e.Item1 == customUniqueId);
            if (typeHandleMapping != default)
            {
                Debug.LogWarning(customUniqueId + " is already registered in TypeSerializer");
                return typeHandleMapping.Item2;
            }

            var typeMapping = s_CustomIdToType.Find(e => e.Item2 == t);
            if (typeMapping != default)
            {
                Debug.LogWarning(t.FullName + " is already registered in TypeSerializer");
            }

            th = new TypeHandle(customUniqueId);

            s_CustomIdToTypeHandle.Add((customUniqueId, th));
            s_CustomIdToType.Add((customUniqueId, t));

            return th;
        }

        public static TypeHandle GenerateTypeHandle<T>()
        {
            return GenerateTypeHandle(typeof(T));
        }

        public static TypeHandle GenerateTypeHandle(Type t)
        {
            return new TypeHandle(Serialize(t));
        }
    }
}
