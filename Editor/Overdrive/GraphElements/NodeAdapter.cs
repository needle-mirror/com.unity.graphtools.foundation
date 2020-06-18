using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    // types of port to adapt
    public class PortSource<T>
    {
    }

    public class NodeAdapter
    {
        static List<MethodInfo> s_TypeAdapters;
        static Dictionary<int, MethodInfo> s_NodeAdapterDictionary;
        static Dictionary<Type, object> s_PortSources;

        object GetPortSource(IGTFPortModel portModel)
        {
            if (s_PortSources == null)
            {
                s_PortSources = new Dictionary<Type, object>();
            }

            if (s_PortSources.TryGetValue(portModel.PortDataType, out var source))
                return source;

            Type genericClass = typeof(PortSource<>);
            Type constructedClass = genericClass.MakeGenericType(portModel.PortDataType);
            source = Activator.CreateInstance(constructedClass);
            s_PortSources[portModel.PortDataType] = source;
            return source;
        }

        IEnumerable<MethodInfo> GetExtensionMethods(Assembly assembly, Type extendedType)
        {
            return assembly.GetTypes()
                .Where(t => t.IsSealed && !t.IsGenericType && !t.IsNested)
                .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                .Where(m => m.IsDefined(typeof(ExtensionAttribute), false) && m.GetParameters()[0].ParameterType == extendedType);
        }

        public MethodInfo GetAdapter(IGTFPortModel portA, IGTFPortModel portB)
        {
            if (portA == null || portB == null)
                return null;

            var a = GetPortSource(portA);
            var b = GetPortSource(portB);

            if (a == null || b == null)
                return null;

            if (s_NodeAdapterDictionary == null)
            {
                s_NodeAdapterDictionary = new Dictionary<int, MethodInfo>();

                // add extension methods
                AppDomain currentDomain = AppDomain.CurrentDomain;
                foreach (Assembly assembly in currentDomain.GetAssemblies())
                {
                    IEnumerable<MethodInfo> methods;

                    try
                    {
                        methods = GetExtensionMethods(assembly, typeof(NodeAdapter));
                    }
                    // Invalid DLLs might raise this exception, simply ignore it
                    catch (ReflectionTypeLoadException)
                    {
                        continue;
                    }

                    foreach (MethodInfo method in methods)
                    {
                        ParameterInfo[] methodParams = method.GetParameters();
                        if (methodParams.Length == 3)
                        {
                            string pa = methodParams[1].ParameterType + methodParams[2].ParameterType.ToString();
                            int hash = pa.GetHashCode();
                            if (s_NodeAdapterDictionary.ContainsKey(hash))
                            {
                                Debug.Log("NodeAdapter: multiple extensions have the same signature:\n" +
                                    "1: " + method + "\n" +
                                    "2: " + s_NodeAdapterDictionary[hash]);
                            }
                            else
                            {
                                s_NodeAdapterDictionary.Add(hash, method);
                            }
                        }
                    }
                }
            }

            string s = a.GetType().ToString() + b.GetType();

            return s_NodeAdapterDictionary.TryGetValue(s.GetHashCode(), out var methodInfo) ? methodInfo : null;
        }
    }
}
