using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    // Use this attribute to tag classes containing static extension methods you want to cache in an ExtensionMethodCache
    [MeansImplicitUse(ImplicitUseTargetFlags.WithMembers)]
    public class GraphElementsExtensionMethodsCacheAttribute : Attribute
    {
        public const int lowestPriority = 0;
        public const int toolDefaultPriority = 1;

        public int Priority { get; }

        public GraphElementsExtensionMethodsCacheAttribute(int priority = toolDefaultPriority)
        {
            Priority = priority;
        }
    }

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

                    if (!factoryMethods.TryGetValue(key, out var currentValue))
                    {
                        factoryMethods[key] = methodInfo;
                    }
                    else
                    {
                        int currentPriority = 0;
                        if (currentValue.DeclaringType != null)
                        {
                            var methodCacheAttr = (GraphElementsExtensionMethodsCacheAttribute)Attribute.GetCustomAttribute(currentValue.DeclaringType, typeof(GraphElementsExtensionMethodsCacheAttribute));
                            currentPriority = methodCacheAttr.Priority;
                        }

                        int priority = 0;
                        if (methodInfo.DeclaringType != null)
                        {
                            var methodCacheAttr = (GraphElementsExtensionMethodsCacheAttribute)Attribute.GetCustomAttribute(methodInfo.DeclaringType, typeof(GraphElementsExtensionMethodsCacheAttribute));
                            priority = methodCacheAttr.Priority;
                        }

                        if (priority == currentPriority)
                        {
                            Debug.LogError($"Duplicate extension methods for type {key} have the same priority" +
                                $"as a previously discovered extension method. It will be ignored." +
                                $" Previous value: {currentValue}, new value: {methodInfo}, extended type: {extendedType.FullName}");
                        }
                        else if (priority < currentPriority)
                        {
                            var gtfAssembly = typeof(GraphElementsExtensionMethodsCacheAttribute).Assembly;
                            var newMethodAssembly = methodInfo.DeclaringType?.Assembly;
                            if (newMethodAssembly != gtfAssembly)
                            {
                                Debug.LogError($"Extension methods for type {key} has lower priority than an" +
                                    $"extension method declared in GraphToolsFoundation. It will be ignored." +
                                    $" Previous value: {currentValue}, new value: {methodInfo}, extended type: {extendedType.FullName}");
                            }
                        }
                        else if (priority > currentPriority)
                        {
                            factoryMethods[key] = methodInfo;
                        }
                    }
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
