using System;

namespace UnityEditor.VisualScripting.Runtime
{
    [AttributeUsage(AttributeTargets.Field)]
    public class TypeSearcherAttribute : Attribute
    {
        public Type FilteredType { get; protected set; }

        public TypeSearcherAttribute(Type filter = null)
        {
            FilteredType = filter;
        }
    }
}
