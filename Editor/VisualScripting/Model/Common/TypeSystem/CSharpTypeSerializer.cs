using System;
using System.Collections.Generic;

namespace UnityEditor.VisualScripting.Model.Stencils
{
    public class CSharpTypeSerializer
    {
        public readonly Dictionary<string, string> typeRenames;

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
                retType = Type.GetType(assemblyQualifiedName) ?? retType;
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
