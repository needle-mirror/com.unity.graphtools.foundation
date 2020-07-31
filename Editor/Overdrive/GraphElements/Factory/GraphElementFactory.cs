using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    public static class GraphElementFactory
    {
        static Dictionary<ValueTuple<GraphView, IGTFGraphElementModel>, IGraphElement> s_UIForModel = new Dictionary<ValueTuple<GraphView, IGTFGraphElementModel>, IGraphElement>();

        [CanBeNull]
        public static T GetUI<T>(this IGTFGraphElementModel model, GraphView graphView) where T : class, IGraphElement
        {
            return s_UIForModel.TryGetValue(new ValueTuple<GraphView, IGTFGraphElementModel>(graphView, model), out var ui) ? ui as T : null;
        }

        [CanBeNull]
        internal static GraphElement GetUI(this IGTFGraphElementModel model, GraphView graphView)
        {
            return s_UIForModel.TryGetValue(new ValueTuple<GraphView, IGTFGraphElementModel>(graphView, model), out var ui) ? ui as GraphElement : null;
        }

        [CanBeNull]
        public static T CreateUI<T>(this IGTFGraphElementModel model, GraphView graphView, Overdrive.Store store) where T : class, IGraphElement
        {
            return CreateUI<T>(graphView, store, model);
        }

        public static T CreateUI<T>(GraphView graphView, Overdrive.Store store, IGTFGraphElementModel model) where T : class, IGraphElement
        {
            if (model == null)
            {
                Debug.LogError("GraphElementFactory could not create element because model is null.");
                return null;
            }

            var ext = ExtensionMethodCache<ElementBuilder>.GetExtensionMethod(
                model.GetType(),
                FilterMethods,
                KeySelector
            );

            T newElem = null;
            if (ext != null)
            {
                var nodeBuilder = new ElementBuilder { GraphView = graphView };
                newElem = ext.Invoke(null, new object[] { nodeBuilder, store, model }) as T;
            }

            if (newElem == null)
            {
                Debug.LogError($"GraphElementFactory doesn't know how to create a UI for element of type: {model.GetType()}");
                return null;
            }

            s_UIForModel[new ValueTuple<GraphView, IGTFGraphElementModel>(graphView, model)] = newElem;

            return newElem;
        }

        internal static Type KeySelector(MethodInfo x)
        {
            return x.GetParameters()[2].ParameterType;
        }

        internal static bool FilterMethods(MethodInfo x)
        {
            if (x.ReturnType != typeof(IGraphElement))
                return false;

            var parameters = x.GetParameters();
            return parameters.Length == 3 && parameters[1].ParameterType == typeof(Overdrive.Store);
        }

        public static void RemoveAll(GraphView graphView)
        {
            var toRemove = s_UIForModel.Where(pair => pair.Key.Item1 == graphView).Select(pair => pair.Key).ToList();

            foreach (var key in toRemove)
            {
                s_UIForModel.Remove(key);
            }
        }
    }
}
