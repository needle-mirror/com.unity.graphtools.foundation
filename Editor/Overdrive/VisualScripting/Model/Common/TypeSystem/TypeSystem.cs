using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public static class TypeSystem
    {
        static readonly Regex k_CodifyRegex = new Regex("[^a-zA-Z0-9]", RegexOptions.Compiled);

        public static string CodifyString(string str)
        {
            return k_CodifyRegex.Replace(str, "_");
        }

        // Create a dictionary of every method in a class tagged with TAttribute, grouped by the type of their first parameter type
        // [MyAttr] class Foo { public void A(float); public void B(int); public void C(float); public void D() }
        // GetExtensionMethods<MyAttrAttribute>() =>  { float => {A, C}, int => B }
        public static Dictionary<Type, List<MethodInfo>> GetExtensionMethods<TAttribute>(IEnumerable<Assembly> assemblies) where TAttribute : Attribute
        {
            Type GetMethodFirstParameterType(MethodInfo m) => m.GetParameters()[0].ParameterType.IsArray ? m.GetParameters()[0].ParameterType.GetElementType() : m.GetParameters()[0].ParameterType;
            return TypeCache.GetTypesWithAttribute<TAttribute>()
                .Where(t => assemblies.Contains(t.Assembly) && t.IsClass)
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly))
                .Where(m => m.GetParameters().Length > 0)
                .GroupBy(GetMethodFirstParameterType)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        public static Dictionary<Type, List<MethodInfo>> GetExtensionMethods(IEnumerable<Assembly> assemblies)
        {
            return GetExtensionMethods<ExtensionAttribute>(assemblies);
        }

        static IEnumerable<MethodInfo> GetUserExtensionMethods(Type type)
        {
            var userAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.FullName.IndexOf("Assembly-CSharp") >= 0);
            var extensions = GetExtensionMethods(userAssemblies);

            return extensions[type];
        }
    }
}
