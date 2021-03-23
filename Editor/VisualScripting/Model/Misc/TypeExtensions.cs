using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.VisualScripting;
using Object = UnityEngine.Object;

namespace UnityEditor.VisualScripting.Model
{
    public static class TypeExtensions
    {
        static readonly Dictionary<Type, string> k_TypeToFriendlyName = new Dictionary<Type, string>
        {
            { typeof(string),  "String" },
            { typeof(object),  "System.Object" },
            { typeof(bool),    "Boolean" },
            { typeof(byte),    "Byte" },
            { typeof(char),    "Char" },
            { typeof(decimal), "Decimal" },
            { typeof(double),  "Double" },
            { typeof(short),   "Short" },
            { typeof(int),     "Integer" },
            { typeof(long),    "Long" },
            { typeof(sbyte),   "SByte" },
            { typeof(float),   "Float" },
            { typeof(ushort),  "Unsigned Short" },
            { typeof(uint),    "Unsigned Integer" },
            { typeof(ulong),   "Unsigned Long" },
            { typeof(void),    "Void" },
            { typeof(Color),   "Color"},
            { typeof(Object), "UnityEngine.Object"},
            { typeof(Vector2), "Vector 2"},
            { typeof(Vector3), "Vector 3"},
            { typeof(Vector4), "Vector 4"}
        };

        public static string FriendlyName(this Type type, bool expandGeneric = true)
        {
            if (k_TypeToFriendlyName.TryGetValue(type, out var friendlyName))
            {
                return friendlyName;
            }

            var attribute = type.GetCustomAttribute<VisualScriptingFriendlyNameAttribute>();
            friendlyName = attribute?.FriendlyName ?? type.Name;

            if (type.IsGenericType && expandGeneric)
            {
                int backtick = friendlyName.IndexOf('`');
                if (backtick > 0)
                {
                    friendlyName = friendlyName.Remove(backtick);
                }
                friendlyName += " of ";
                Type[] typeParameters = type.GetGenericArguments();
                for (int i = 0; i < typeParameters.Length; i++)
                {
                    string typeParamName = typeParameters[i].FriendlyName();
                    friendlyName += (i == 0 ? typeParamName : " and " + typeParamName);
                }
            }

            if (type.IsArray)
            {
                return type.GetElementType().FriendlyName() + "[]";
            }

            return friendlyName;
        }

        public static IEnumerable<string> ReferenceNamespaces(this Type type)
        {
            List<string> namespaces = new List<string>();
            if (type.IsGenericType)
                foreach (var a in type.GetGenericArguments())
                    namespaces.AddRange(a.ReferenceNamespaces());
            if (!string.IsNullOrEmpty(type.Namespace))
                namespaces.Add(type.Namespace);
            return namespaces;
        }

        public static bool IsNumeric(this Type self)
        {
            switch (Type.GetTypeCode(self))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }

        public static bool HasNumericConversionTo(this Type self, Type other)
        {
            return self.IsNumeric() && other.IsNumeric();
        }
    }
}
