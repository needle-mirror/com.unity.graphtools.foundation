using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public static class UIForModel
    {
        static GraphElementMapping s_UIForModel = new GraphElementMapping();

        public static void AddOrReplaceGraphElement(IGraphElement graphElement)
        {
            s_UIForModel.AddOrReplaceUIForModel(graphElement);
        }

        [CanBeNull]
        public static T GetUI<T>(this IGraphElementModel model, GraphView graphView, string context = null) where T : class, IGraphElement
        {
            return s_UIForModel.FirstOrDefault(graphView, context, model) as T;
        }

        [CanBeNull]
        internal static IGraphElement GetUI(this IGraphElementModel model, GraphView graphView, string context = null)
        {
            return s_UIForModel.FirstOrDefault(graphView, context, model);
        }

        internal static IEnumerable<IGraphElement> GetAllUIs(this IGraphElementModel model, GraphView graphView)
        {
            return s_UIForModel.GetAllUIForModel(model).Where(ui => ui.GraphView == graphView);
        }

        public static void RemoveGraphElement(IGraphElement graphElement)
        {
            s_UIForModel.RemoveGraphElement(graphElement);
        }

        internal static void Reset()
        {
            s_UIForModel = new GraphElementMapping();
        }
    }
}
