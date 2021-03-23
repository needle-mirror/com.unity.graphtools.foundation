using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public static class TypeSerializer
    {
        static System.Text.RegularExpressions.Regex s_GenericTypeExtractionRegex = new System.Text.RegularExpressions.Regex(@"(?<=\[\[)(.*?)(?=\]\])");

        static List<ValueTuple<string, TypeHandle>> s_CustomIdToTypeHandle = new List<ValueTuple<string, TypeHandle>>();
        static List<ValueTuple<string, Type>> s_CustomIdToType = new List<ValueTuple<string, Type>>();

        static Dictionary<string, Type> s_MovedFromTypes;
        static Dictionary<string, Type> MovedFromTypes
        {
            get
            {
                if (s_MovedFromTypes == null)
                {
                    s_MovedFromTypes = new Dictionary<string, Type>();
                    var movedFromTypes = TypeCache.GetTypesWithAttribute<MovedFromAttribute>();
                    foreach (var t in movedFromTypes)
                    {
                        var attributes = Attribute.GetCustomAttributes(t, typeof(MovedFromAttribute));
                        foreach (var attribute in attributes)
                        {
                            var movedFromAttribute = (MovedFromAttribute)attribute;
                            movedFromAttribute.GetData(out _, out var nameSpace, out var assembly, out var className);

                            var currentClassName = GetFullNameNoNamespace(t.FullName, t.Namespace);
                            var currentNamespace = t.Namespace;
                            var currentAssembly = t.Assembly.GetName().Name;

                            var oldNamespace = string.IsNullOrEmpty(nameSpace) ? currentNamespace : nameSpace;
                            var oldClassName = string.IsNullOrEmpty(className) ? currentClassName : className;
                            var oldAssembly = string.IsNullOrEmpty(assembly) ? currentAssembly : assembly;

                            var oldAssemblyQualifiedName =
                                oldNamespace != null ? $"{oldNamespace}.{oldClassName}, {oldAssembly}" : $"{oldClassName}, {oldAssembly}";
                            s_MovedFromTypes.Add(oldAssemblyQualifiedName, t);
                        }
                    }
                }

                return s_MovedFromTypes;
            }
        }

        /// <summary>
        /// Gets the full name of a type without the namespace.
        /// </summary>
        /// <remarks>
        /// The full name of a type nested type includes the outer class type name. The type names are normally
        /// separated by '+' but Unity serialization uses the '/' character as separator.
        ///
        /// This method returns the full type name of a class and switches the type separator to '/' to follow Unity.
        /// </remarks>
        /// <param name="typeName">The full type name, including the namespace.</param>
        /// <param name="nameSpace">The namespace to be removed.</param>
        /// <returns>Returns a string.</returns>
        static string GetFullNameNoNamespace(string typeName, string nameSpace)
        {
            if (typeName != null && nameSpace != null && typeName.Contains(nameSpace))
            {
                return typeName.Substring(nameSpace.Length + 1).Replace("+", "/");
            }
            return typeName;
        }

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
                    if (MovedFromTypes.ContainsKey(assemblyQualifiedName))
                    {
                        type = MovedFromTypes[assemblyQualifiedName];
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
