using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace UnityEditor.VisualScripting.Model
{
    // Use this attribute to tag classes containing static extension methods you want to cache in an ExtensionMethodCache
    [MeansImplicitUse(ImplicitUseTargetFlags.WithMembers)]
    public class GraphtoolsExtensionMethodsAttribute : Attribute {}

    public static class ModelUtility
    {
        static readonly string k_CompileScriptsOutputDirectory = Path.Combine(Environment.CurrentDirectory, "Library", "VisualScripting");
        static readonly string k_AssemblyRelativePath = Path.Combine("Assets", "Runtime", "VisualScripting");
        static readonly string k_AssemblyOutputDirectory = Path.Combine(Environment.CurrentDirectory, k_AssemblyRelativePath);

        public static string GetCompileScriptsOutputDirectory()
        {
            return k_CompileScriptsOutputDirectory;
        }

        public static string GetAssemblyOutputDirectory()
        {
            return k_AssemblyOutputDirectory;
        }

        public static string GetAssemblyRelativePath()
        {
            return k_AssemblyRelativePath;
        }

        public enum VisitMode
        {
            OnlyClassesWithAttribute, EveryMethod
        }

        public static class ExtensionMethodCache<TExtendedType>
        {
            // ReSharper disable once StaticMemberInGenericType
            static Dictionary<Type, MethodInfo> s_FactoryMethods;

            public static MethodInfo GetExtensionMethod(Type targetType, Func<MethodInfo, bool> filterMethods, Func<MethodInfo, Type> keySelector)
            {
                return GetExtensionMethodOf(targetType, filterMethods, keySelector);
            }

            static MethodInfo GetExtensionMethodOf(Type targetType, Func<MethodInfo, bool> filterMethods, Func<MethodInfo, Type> keySelector)
            {
                if (targetType == typeof(ScriptableObject))
                    return null;

                if (s_FactoryMethods == null)
                {
                    s_FactoryMethods = FindMatchingExtensionMethods(filterMethods, keySelector);
                }

                if (s_FactoryMethods.ContainsKey(targetType))
                    return s_FactoryMethods[targetType];

                MethodInfo extension = null;

                foreach (var type in GetInterfaces(targetType, false))
                {
                    extension = GetExtensionMethodOf(type, filterMethods, keySelector);
                    if (extension != null)
                        break;
                }


                if (extension == null && targetType.BaseType != null)
                    extension = GetExtensionMethodOf(targetType.BaseType, filterMethods, keySelector);

                //Did we find a builder for one of our base class?
                //If so, add it to the dictionary, this will optimize the next call
                //for this type
                if (extension != null)
                    s_FactoryMethods[targetType] = extension;
//                else
//                {
//                    if (extendedType.BaseType != null)
//                        return GetExtensionMethodOf<TAttribute>(extendedType.BaseType, t, filterMethods, keySelector);
//                }

                return extension;
            }

            public static Dictionary<Type, MethodInfo> FindMatchingExtensionMethods(Func<MethodInfo, bool> filterMethods, Func<MethodInfo, Type> keySelector, VisitMode mode = VisitMode.OnlyClassesWithAttribute)
            {
                return mode == VisitMode.OnlyClassesWithAttribute
                    ? FindMatchingExtensionMethods<GraphtoolsExtensionMethodsAttribute>(filterMethods, keySelector) // only goes through methods inside a class with GraphtoolsExtensionMethodsAttribute
                    : FindMatchingExtensionMethods<ExtensionAttribute>(filterMethods, keySelector); // goes through every method. Super slow. Kept for test purposes.
            }

            static Dictionary<Type, MethodInfo> FindMatchingExtensionMethods<TAttribute>(Func<MethodInfo, bool> filterMethods, Func<MethodInfo, Type> keySelector) where TAttribute : Attribute
            {
                var factoryMethods = new Dictionary<Type, MethodInfo>();

                var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => !a.FullName.StartsWith("Microsoft"));
                var extensionMethods = TypeSystem.GetExtensionMethods<TAttribute>(assemblies);
                Type extendedType = typeof(TExtendedType);
                if (extensionMethods.TryGetValue(extendedType, out var allMethodInfos))
                {
                    foreach (var methodInfo in allMethodInfos.Where(filterMethods))
                    {
                        var key = keySelector(methodInfo);
                        if (factoryMethods.TryGetValue(key, out var prevValue))
                        {
                            Debug.LogError($"Duplicate extension methods for type {key}, previous value: {prevValue}, new value: {methodInfo}, extended type: {extendedType.FullName}");
                        }

                        factoryMethods[key] = methodInfo;
                    }
                }

                return factoryMethods;
            }

            static IEnumerable<Type> GetInterfaces(Type type, bool includeInherited)
            {
                if (includeInherited || type.BaseType == null)
                    return type.GetInterfaces();

                return type.GetInterfaces().Except(type.BaseType.GetInterfaces());
            }
        }
    }
}
