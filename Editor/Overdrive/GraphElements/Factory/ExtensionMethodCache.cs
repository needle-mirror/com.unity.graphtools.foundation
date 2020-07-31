using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    // Use this attribute to tag classes containing static extension methods you want to cache in an ExtensionMethodCache
    [MeansImplicitUse(ImplicitUseTargetFlags.WithMembers)]
    public class GraphElementsExtensionMethodsCacheAttribute : Attribute {}

    enum ExtensionMethodCacheVisitMode
    {
        OnlyClassesWithAttribute, EveryMethod
    }

    public static class ExtensionMethodCache<TExtendedType>
    {
        // ReSharper disable once StaticMemberInGenericType
        static Dictionary<Type, MethodInfo> s_FactoryMethods;

        static Queue<Type> s_CandidateTypes = new Queue<Type>();

        public static MethodInfo GetExtensionMethod(Type targetType, Func<MethodInfo, bool> filterMethods, Func<MethodInfo, Type> keySelector)
        {
            s_CandidateTypes.Clear();
            s_CandidateTypes.Enqueue(targetType);
            var extension = GetExtensionMethodOf(s_CandidateTypes, filterMethods, keySelector);
            while (extension == null && s_CandidateTypes.Any())
            {
                extension = GetExtensionMethodOf(s_CandidateTypes, filterMethods, keySelector);
            }

            if (!s_FactoryMethods.ContainsKey(targetType))
                s_FactoryMethods[targetType] = extension;

            return extension;
        }

        static MethodInfo GetExtensionMethodOf(Queue<Type> candidateTypes, Func<MethodInfo, bool> filterMethods, Func<MethodInfo, Type> keySelector)
        {
            if (candidateTypes == null || !candidateTypes.Any())
                return null;

            var targetType = candidateTypes.Dequeue();

            if (targetType == typeof(ScriptableObject))
                return null;

            if (s_FactoryMethods == null)
            {
                s_FactoryMethods = FindMatchingExtensionMethods(filterMethods, keySelector);
            }

            if (s_FactoryMethods.ContainsKey(targetType))
                return s_FactoryMethods[targetType];

            if (!targetType.IsInterface)
            {
                foreach (var type in GetInterfaces(targetType))
                {
                    candidateTypes.Enqueue(type);
                }

                if (targetType.BaseType != null)
                {
                    candidateTypes.Enqueue(targetType.BaseType);
                }
            }

            return null;
        }

        internal static Dictionary<Type, MethodInfo> FindMatchingExtensionMethods(Func<MethodInfo, bool> filterMethods, Func<MethodInfo, Type> keySelector, ExtensionMethodCacheVisitMode mode = ExtensionMethodCacheVisitMode.OnlyClassesWithAttribute)
        {
            return mode == ExtensionMethodCacheVisitMode.OnlyClassesWithAttribute
                ? FindMatchingExtensionMethods<GraphElementsExtensionMethodsCacheAttribute>(filterMethods, keySelector) // only goes through methods inside a class with GraphElementsExtensionMethodsCacheAttribute
                : FindMatchingExtensionMethods<ExtensionAttribute>(filterMethods, keySelector); // goes through every method. Super slow. Kept for test purposes.
        }

        static Dictionary<Type, MethodInfo> FindMatchingExtensionMethods<TAttribute>(Func<MethodInfo, bool> filterMethods, Func<MethodInfo, Type> keySelector) where TAttribute : Attribute
        {
            var factoryMethods = new Dictionary<Type, MethodInfo>();

            var assemblies = AssemblyCache.CachedAssemblies;
            var extensionMethods = AssemblyCache.GetExtensionMethods<TAttribute>(assemblies);
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

        static IEnumerable<Type> GetInterfaces(Type type)
        {
            if (type.BaseType == null)
                return type.GetInterfaces();

            return type.GetInterfaces().Except(type.BaseType.GetInterfaces());
        }
    }
}
