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

                            var currentClassName = t.Name;
                            var currentNamespace = t.Namespace;
                            var currentAssembly = t.Assembly.FullName;

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
                    // check if the type has moved
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
