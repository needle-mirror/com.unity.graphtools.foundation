using System;
using System.Collections.Generic;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.VisualScripting.Model.Stencils
{
    public class CSharpTypeSerializer
    {
        public readonly Dictionary<string, string> typeRenames;

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

                            var newNamespace = string.IsNullOrEmpty(nameSpace) ? currentNamespace : nameSpace;
                            var newClassName = string.IsNullOrEmpty(className) ? currentClassName : className;
                            var newAssembly = string.IsNullOrEmpty(assembly) ? currentAssembly : assembly;

                            var str = $"{newNamespace}.{newClassName}, {newAssembly}";

                            s_MovedFromTypes.Add(str, t);
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
            if (typeName.Contains(nameSpace))
            {
                return typeName.Substring(nameSpace.Length + 1).Replace("+", "/");
            }

            return typeName;
        }

        public CSharpTypeSerializer(Dictionary<string, string> typeRenames = null)
        {
            this.typeRenames = typeRenames ?? new Dictionary<string, string>();
        }

        static string Serialize(Type t)
        {
            //TODO Get rid of these C# types and handle these cases in the translator directly via metadata (?)
            if (t == typeof(ExecutionFlow))
            {
                return TypeHandle.ExecutionFlow.Identification;
            }
            if (t == typeof(Unknown))
            {
                return TypeHandle.Unknown.Identification;
            }
            return t.AssemblyQualifiedName;
        }

        Type Deserialize(string serializedType)
        {
            Type typeResolved;
            switch (serializedType)
            {
                case "__EXECUTIONFLOW":
                    typeResolved = typeof(ExecutionFlow);
                    break;

                case "__UNKNOWN":
                    typeResolved = typeof(Unknown);
                    break;

                default:
                    typeResolved = GetTypeFromName(serializedType);
                    break;
            }
            return typeResolved;
        }

        Type GetTypeFromName(string assemblyQualifiedName)
        {
            Type retType = typeof(Unknown);
            if (!string.IsNullOrEmpty(assemblyQualifiedName))
            {
                if (typeRenames != null && typeRenames.TryGetValue(assemblyQualifiedName, out var newName))
                    assemblyQualifiedName = newName;

                var type = Type.GetType(assemblyQualifiedName);
                if (type == null)
                {
                    // Check if the type has moved

                    // remove the assembly version string
                    var versionIdx = assemblyQualifiedName.IndexOf(", Version=");
                    if (versionIdx > 0)
                        assemblyQualifiedName = assemblyQualifiedName.Substring(0, versionIdx);
                    // replace all '+' with '/' to follow the Unity serialization convention for nested types
                    assemblyQualifiedName = assemblyQualifiedName.Replace("+", "/");
                    if (MovedFromTypes.ContainsKey(assemblyQualifiedName))
                    {
                        type = MovedFromTypes[assemblyQualifiedName];
                    }
                }

                retType = type ?? retType;
            }
            return retType;
        }

        public Type ResolveType(TypeHandle th)
        {
            return Deserialize(th.Identification);
        }

        public TypeHandle GenerateTypeHandle(Type t)
        {
            return new TypeHandle(Serialize(t));
        }
    }
}
