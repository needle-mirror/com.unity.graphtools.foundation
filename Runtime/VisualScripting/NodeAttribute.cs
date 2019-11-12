using System;
using JetBrains.Annotations;

namespace UnityEngine.VisualScripting
{
    [PublicAPI]
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
    public class NodeAttribute : Attribute
    {
        Type[] BackendTypes { get; }

        public bool IsCompatible(Type t)
        {
            if (BackendTypes.Length == 0)
                return true;
            for (int a = 0; a < BackendTypes.Length; a++)
            {
                if (BackendTypes[a] == t)
                    return true;
            }

            return false;
        }

        public NodeAttribute()
        {
            BackendTypes = new Type[0];
        }

        public NodeAttribute(Type type)
        {
            BackendTypes = new[] { type };
        }

        public NodeAttribute(Type[] types)
        {
            BackendTypes = types;
        }
    }
}
